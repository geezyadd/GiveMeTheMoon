using System;

namespace Features.MatrixModule.Scripts.Exceptions {
    public class InfiniteLoopTypeException : Exception {
        private const string INFINITE_LOOP_TYPE_EXCEPTION = "The code entered an infinite loop!";
        public InfiniteLoopTypeException() : base(INFINITE_LOOP_TYPE_EXCEPTION) { }
    }
}