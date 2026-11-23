using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace FooProject.Collection
{
    // Source - https://stackoverflow.com/a/75291475
    // Posted by Prince Owen, modified by community. See post 'Timeline' for change history
    // Retrieved 2026-11-23, License - CC BY-SA 4.0

    internal class ChunkedSequence<T>
    {
        private ReadOnlyChunk<T> _first;
        private ReadOnlyChunk<T> _current;

        private bool _changed;
        private ReadOnlySequence<T>? _sequence;

        public ChunkedSequence()
        {
            _first = _current = null;
            _sequence = null;
            _changed = false;
        }

        public ChunkedSequence(ReadOnlySequence<T> sequence) : this()
        {
            Append(sequence);
        }

        public void Append(ReadOnlySequence<T> sequence)
        {
            SequencePosition pos = sequence.Start;
            while (sequence.TryGet(ref pos, out ReadOnlyMemory<T> mem, true))
            {
                Append(mem);
            }
        }

        public void Append(ReadOnlyMemory<T> memory)
        {
            if (_current == null)
            {
                _first = _current = new ReadOnlyChunk<T>(memory);
            }
            else
            {
                _current = _current.Append(memory);
            }

            _changed = true;
        }

        internal ReadOnlySequence<T> GetSequence()
        {
            if (_changed)
            {
                _sequence = new ReadOnlySequence<T>(_first, 0, _current, _current.Memory.Length);
            }
            else if (_sequence == null)
            {
                _sequence = new ReadOnlySequence<T>();
            }

            return _sequence.Value;
        }

        private sealed class ReadOnlyChunk<_T> : ReadOnlySequenceSegment<_T>
        {
            public ReadOnlyChunk(ReadOnlyMemory<_T> memory)
            {
                Memory = memory;
            }

            public ReadOnlyChunk<_T> Append(ReadOnlyMemory<_T> memory)
            {
                ReadOnlyChunk<_T> nextChunk = new ReadOnlyChunk<_T>(memory)
                {
                    RunningIndex = RunningIndex + Memory.Length
                };

                Next = nextChunk;
                return nextChunk;
            }
        }
    }
}
