using LibraryTraining.Application.BookCopies;
using LibraryTraining.Infrastructure.BookCopies;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LibraryTraining.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddControllers();
            // 接続設定の読み込みは起動側に置き、Oracle実装へ文字列を渡す。
            builder.Services.AddScoped<IBookCopyQuery>(provider => new OracleBookCopyQuery(
                provider.GetRequiredService<IConfiguration>().GetConnectionString("LibraryOracle")));
            builder.Services.AddScoped<GetBookCopiesUseCase>();
            builder.Services.AddOpenApi();

            var app = builder.Build();
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }
            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();
            app.Run();
        }
    }
}