using LibraryTraining.Application.BookCopies;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LibraryTraining.Infrastructure.BookCopies
{
    // 学習用の固定データ。Oracleの貸出可否判定やSQLを検証するものではない。
    public class FakeBookCopyQuery : IBookCopyQuery
    {
        public Task<IReadOnlyList<BookCopyDto>> GetAllAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            IReadOnlyList<BookCopyDto> copies = new[]
            {
                Create("copy-001", "book-a", "C-001", "A", "技術", "A-01", BookCopyAvailability.Available),
                Create("copy-002", "book-a", "C-002", "A", "技術", "A-01", BookCopyAvailability.OnLoan),
                Create("copy-003", "book-b", "C-003", "B", "技術", "A-02", BookCopyAvailability.Available),
                Create("copy-004", "book-c", "C-004", "C", "文学", "B-01", BookCopyAvailability.OnLoan),
                Create("copy-005", "book-d", "C-005", "D", "文学", null, BookCopyAvailability.Available)
            };

            return Task.FromResult(copies);
        }

        private static BookCopyDto Create(
            string copyId, string bookId, string accessionNumber, string sample,
            string category, string shelfLocation, BookCopyAvailability availability)
        {
            return new BookCopyDto
            {
                BookCopyId = copyId,
                BookId = bookId,
                AccessionNumber = accessionNumber,
                Title = "図書館サンプル" + sample,
                AuthorDisplayName = "サンプル著者" + sample,
                Category = category,
                ShelfLocation = shelfLocation,
                Availability = availability
            };
        }
    }
}
