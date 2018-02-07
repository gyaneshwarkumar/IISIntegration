using Microsoft.AspNetCore.HttpSys.Internal;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Server.IISIntegration
{
    internal partial class IISHttpContext
    {
        private IISAwaitable _readWebSocketsOperation;
        private IISAwaitable _writeWebSocketsOperation;

        protected Task _readWebsocketTask;
        protected Task _writeWebsocketTask;

        private TaskCompletionSource<object> _upgradeTcs;

        public async Task UpgradeAsync()
        {
            if (_upgradeTcs == null)
            {
                _upgradeTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
                await FlushAsync();
                await _upgradeTcs.Task;
            }
        }

        private unsafe IISAwaitable ReadWebSocketsAsync(int length)
        {
            try
            {
                var hr = 0;
                int dwReceivedBytes;
                bool fCompletionExpected;
                hr = NativeMethods.http_websockets_read_bytes(
                                          _pInProcessHandler,
                                          (byte*)_inputHandle.Pointer,
                                          length,
                                          IISAwaitable.ReadCallback,
                                          (IntPtr)_thisHandle,
                                          out dwReceivedBytes,
                                          out fCompletionExpected);
                if (!fCompletionExpected)
                {
                    CompleteReadWebSockets(hr, dwReceivedBytes);
                }
            }
            catch (NullReferenceException)
            {

            }

            return _readWebSocketsOperation;
        }

        private unsafe IISAwaitable WriteWebSocketsAsync(ReadOnlyBuffer<byte> buffer)
        {
            var fCompletionExpected = false;
            var hr = 0;
            var nChunks = 0;

            if (buffer.IsSingleSegment)
            {
                nChunks = 1;
            }
            else
            {
                foreach (var memory in buffer)
                {
                    nChunks++;
                }
            }

            if (buffer.IsSingleSegment)
            {
                var pDataChunks = stackalloc HttpApiTypes.HTTP_DATA_CHUNK[1];

                fixed (byte* pBuffer = &MemoryMarshal.GetReference(buffer.First.Span))
                {
                    ref var chunk = ref pDataChunks[0];

                    chunk.DataChunkType = HttpApiTypes.HTTP_DATA_CHUNK_TYPE.HttpDataChunkFromMemory;
                    chunk.fromMemory.pBuffer = (IntPtr)pBuffer;
                    chunk.fromMemory.BufferLength = (uint)buffer.Length;
                    hr = NativeMethods.http_websockets_write_bytes(_pInProcessHandler, pDataChunks, nChunks, IISAwaitable.WriteCallback, (IntPtr)_thisHandle, out fCompletionExpected);
                }
            }
            else
            {
                // REVIEW: Do we need to guard against this getting too big? It seems unlikely that we'd have more than say 10 chunks in real life
                var pDataChunks = stackalloc HttpApiTypes.HTTP_DATA_CHUNK[nChunks];
                var currentChunk = 0;

                // REVIEW: We don't really need this list since the memory is already pinned with the default pool,
                // but shouldn't assume the pool implementation right now. Unfortunately, this causes a heap allocation...
                var handles = new MemoryHandle[nChunks];

                foreach (var b in buffer)
                {
                    ref var handle = ref handles[currentChunk];
                    ref var chunk = ref pDataChunks[currentChunk];

                    handle = b.Retain(true);

                    chunk.DataChunkType = HttpApiTypes.HTTP_DATA_CHUNK_TYPE.HttpDataChunkFromMemory;
                    chunk.fromMemory.BufferLength = (uint)b.Length;
                    chunk.fromMemory.pBuffer = (IntPtr)handle.Pointer;

                    currentChunk++;
                }
                hr = NativeMethods.http_websockets_write_bytes(_pInProcessHandler, pDataChunks, nChunks, IISAwaitable.WriteCallback, (IntPtr)_thisHandle, out fCompletionExpected);
                // Free the handles
                // TODO make this dispose happen when the async operation completes
                foreach (var handle in handles)
                {
                    handle.Dispose();
                }
            }

            if (!fCompletionExpected)
            {
                _operation.Complete(hr, 0);
            }
            return _operation;
        }

        internal void CompleteWriteWebSockets(int hr, int cbBytes)
        {
            _writeWebSocketsOperation.Complete(hr, cbBytes);
        }

        internal void CompleteReadWebSockets(int hr, int cbBytes)
        {
            _readWebSocketsOperation.Complete(hr, cbBytes);
        }

        private async Task ReadWebSockets()
        {
            try
            {
                while (true)
                {
                    var wb = Input.Writer.GetMemory(MinAllocBufferSize);
                    _inputHandle = wb.Retain(true);

                    try
                    {
                        int read = 0;
                        read = await ReadWebSocketsAsync(wb.Length);

                        if (read == 0)
                        {
                            break;
                        }

                        Input.Writer.Advance(read);
                    }
                    finally
                    {
                        Input.Writer.Commit();
                        _inputHandle.Dispose();
                    }

                    var result = await Input.Writer.FlushAsync();

                    if (result.IsCompleted || result.IsCancelled)
                    {
                        break;
                    }
                }

                Input.Writer.Complete();
            }
            catch (Exception ex)
            {
                Input.Writer.Complete(ex);
            }
        }

        private async Task WriteWebSockets()
        {
            while (true)
            {
                ReadResult result;

                try
                {
                    result = await Output.Reader.ReadAsync();
                }
                catch
                {
                    Output.Reader.Complete();
                    return;
                }

                var buffer = result.Buffer;
                var consumed = buffer.End;

                try
                {
                    if (result.IsCancelled)
                    {
                        break;
                    }

                    if (!buffer.IsEmpty)
                    {
                        await WriteWebSocketsAsync(buffer);
                    }
                    else if (result.IsCompleted)
                    {
                        break;
                    }
                }
                finally
                {
                    Output.Reader.AdvanceTo(consumed);
                }
            }
            Output.Reader.Complete();
        }
    }

}
