using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LibraryTraining.Application.BookCopies
{
    public class BookCopyDto
    {
        [Required]
        public string BookCopyId { get; set; }

        [Required]
        public string BookId { get; set; }

        [Required]
        public string AccessionNumber { get; set; }

        [Required]
        public string Title { get; set; }

        [Required]
        public string AuthorDisplayName { get; set; }

        [Required]
        public string Category { get; set; }

        [JsonRequired]
        public string ShelfLocation { get; set; }

        [JsonRequired]
        public BookCopyAvailability Availability { get; set; }
    }

    [JsonConverter(typeof(BookCopyAvailabilityJsonConverter))]
    public enum BookCopyAvailability
    {
        [JsonStringEnumMemberName("available")]
        Available,

        [JsonStringEnumMemberName("onLoan")]
        OnLoan
    }

    public class BookCopyAvailabilityJsonConverter : JsonStringEnumConverter<BookCopyAvailability>
    {
        public BookCopyAvailabilityJsonConverter() : base(null, false)
        {
        }
    }
}
