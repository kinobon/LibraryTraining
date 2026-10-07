using System;

namespace LibraryTraining.Application.BookCopies
{
    // 取得実装が、一時的な取得不能と識別できた場合に使う。
    public class CatalogUnavailableException : Exception
    {
        public CatalogUnavailableException(string message) : base(message)
        {
        }

        public CatalogUnavailableException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
