using LibraryTraining.Application.BookCopies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LibraryTraining.Api.Controllers
{
    [ApiController]
    [Route("book-copies")]
    public class BookCopiesController : ControllerBase
    {
        private readonly GetBookCopiesUseCase _useCase;
        private readonly ILogger<BookCopiesController> _logger;

        public BookCopiesController(GetBookCopiesUseCase useCase, ILogger<BookCopiesController> logger)
        {
            _useCase = useCase;
            _logger = logger;
        }

        [HttpGet(Name = "GetBookCopies")]
        [ProducesResponseType(typeof(BookCopyDto[]), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IReadOnlyList<BookCopyDto>>> Get(CancellationToken cancellationToken)
        {
            if (Request.Query.Count > 0)
            {
                return CreateProblem(400, "unsupported_query", "このAPIはクエリパラメータに対応していません。");
            }

            try
            {
                var copies = await _useCase.ExecuteAsync(cancellationToken);
                return Ok(copies);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (CatalogUnavailableException exception)
            {
                _logger.LogError(exception, "蔵書一覧を一時的に取得できません。");
                return CreateProblem(503, "catalog_unavailable", "蔵書一覧を一時的に取得できません。");
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "蔵書一覧の取得に失敗しました。");
                return CreateProblem(500, "internal_error", "蔵書一覧の取得に失敗しました。");
            }
        }

        private ObjectResult CreateProblem(int status, string code, string title)
        {
            var problem = new ProblemDetails { Status = status, Title = title };
            problem.Extensions["code"] = code;

            var result = new ObjectResult(problem) { StatusCode = status };
            result.ContentTypes.Add("application/problem+json");
            return result;
        }
    }
}
