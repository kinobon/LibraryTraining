using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LibraryTraining.Application.BookCopies
{
    public interface IBookCopyQuery
    {
        Task<IReadOnlyList<BookCopyDto>> GetAllAsync(CancellationToken cancellationToken);
    }
}
