using LibraryTraining.Application.BookCopies;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LibraryTraining.Infrastructure.BookCopies
{
    public class OracleBookCopyQuery : IBookCopyQuery
    {
        // LOANはEXISTSで調べる。返却済み履歴が複数あっても一冊を一行で返す。
        private const string SelectSql = @"
SELECT c.BOOK_COPY_ID, c.BOOK_ID, c.ACCESSION_NUMBER,
       b.TITLE, b.AUTHOR_DISPLAY_NAME, b.CATEGORY, c.SHELF_LOCATION,
       CASE WHEN EXISTS (
           SELECT 1 FROM LOAN l
           WHERE l.BOOK_COPY_ID = c.BOOK_COPY_ID AND l.RETURNED_AT IS NULL
       ) THEN 'onLoan' ELSE 'available' END AS AVAILABILITY
FROM BOOK_COPY c
INNER JOIN BOOK b ON b.BOOK_ID = c.BOOK_ID
ORDER BY c.ACCESSION_NUMBER, c.BOOK_COPY_ID";

        private readonly string _connectionString;

        public OracleBookCopyQuery(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<IReadOnlyList<BookCopyDto>> GetAllAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(_connectionString))
            {
                // 設定不足は一時障害や空データと区別する。Controllerで500へ変換する。
                throw new InvalidOperationException("ConnectionStrings:LibraryOracleが未設定です。");
            }

            try
            {
                // 公式asyncサンプルのOpenAsync / ExecuteReaderAsyncの形を使い、
                // C# 7.3に合わせて通常のusingブロックでリソースを解放する。
                using (var connection = new OracleConnection(_connectionString))
                {
                    await connection.OpenAsync(cancellationToken);
                    using (var command = new OracleCommand(SelectSql, connection))
                    {
                        command.BindByName = true;
                        command.CommandTimeout = 30;
                        using (var reader = await command.ExecuteReaderAsync(cancellationToken))
                        {
                            var copies = new List<BookCopyDto>();
                            while (await reader.ReadAsync(cancellationToken))
                            {
                                copies.Add(new BookCopyDto
                                {
                                    BookCopyId = reader.GetString(0),
                                    BookId = reader.GetString(1),
                                    AccessionNumber = reader.GetString(2),
                                    Title = reader.GetString(3),
                                    AuthorDisplayName = reader.GetString(4),
                                    Category = reader.GetString(5),
                                    ShelfLocation = reader.IsDBNull(6) ? null : reader.GetString(6),
                                    Availability = reader.GetString(7) == "onLoan"
                                        ? BookCopyAvailability.OnLoan : BookCopyAvailability.Available
                                });
                            }
                            return copies;
                        }
                    }
                }
            }
            catch (OracleException exception) when (cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException("蔵書一覧の取得が中断されました。", exception, cancellationToken);
            }
            catch (OracleException exception) when (IsTemporaryFailure(exception.Number))
            {
                throw new CatalogUnavailableException("Oracleとの通信で蔵書一覧を取得できません。", exception);
            }
        }

        private static bool IsTemporaryFailure(int number)
        {
            // 接続タイムアウト、listener停止、既存接続の切断に限定する。
            // SQL誤り・表の不足・認証失敗等は元の例外を伝え、Controllerで500にする。
            return number == 12170 || number == 12541 || number == 3113
                || number == 3114 || number == 3135;
        }
    }
}
