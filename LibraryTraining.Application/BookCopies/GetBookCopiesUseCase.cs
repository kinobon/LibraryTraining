using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LibraryTraining.Application.BookCopies
{
    public class GetBookCopiesUseCase
    {
        private readonly IBookCopyQuery _query;

        public GetBookCopiesUseCase(IBookCopyQuery query)
        {
            _query = query;
        }

        public async Task<IReadOnlyList<BookCopyDto>> ExecuteAsync(CancellationToken cancellationToken)
        {
            var copies = await _query.GetAllAsync(cancellationToken);

            return copies
                .OrderBy(copy => copy.AccessionNumber, StringComparer.Ordinal)
                .ThenBy(copy => copy.BookCopyId, StringComparer.Ordinal)
                .ToArray();
        }
    }
}
