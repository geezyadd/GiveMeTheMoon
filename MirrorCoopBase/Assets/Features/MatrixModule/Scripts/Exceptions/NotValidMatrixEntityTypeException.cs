using System;

namespace Features.MatrixModule.Scripts.Exceptions {
    public class NotValidMatrixEntityTypeException : Exception {
        private const string NOT_VALID_MATRIX_ENTITY_TYPE_EXCEPTION = "type does not implement ICloneable.";
        public NotValidMatrixEntityTypeException(Type type) : base(type.Name + NOT_VALID_MATRIX_ENTITY_TYPE_EXCEPTION) { }
    }
}