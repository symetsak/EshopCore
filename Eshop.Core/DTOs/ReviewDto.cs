using System;
using System.ComponentModel.DataAnnotations;

namespace Eshop.Application.DTOs
{
    // 1. Το DTO για όταν ο πελάτης ΣΤΕΛΝΕΙ μια κριτική (Request)
    public class CreateReviewDto
    {
        [Required(ErrorMessage = "Η βαθμολογία είναι υποχρεωτική.")]
        [Range(1, 5, ErrorMessage = "Η βαθμολογία πρέπει να είναι από 1 έως 5 αστέρια.")]
        public int Rating { get; set; }

        [StringLength(100, ErrorMessage = "Ο τίτλος δεν μπορεί να ξεπερνά τους 100 χαρακτήρες.")]
        public string? Title { get; set; }

        [StringLength(1000, ErrorMessage = "Το σχόλιο δεν μπορεί να ξεπερνά τους 1000 χαρακτήρες.")]
        public string? Comment { get; set; }
    }

    // 2. Το DTO για όταν το API ΕΠΙΣΤΡΕΦΕΙ την κριτική στο site (Response)
    public class ReviewResponseDto
    {
        public int Id { get; set; }

        public string CustomerId { get; set; } = null!;

        public int Rating { get; set; }

        public string? Title { get; set; }

        public string? Comment { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    // 3. Το Container που επιστρέφει τις κριτικές ΜΑΖΙ με τα στατιστικά (Μέσο Όρο & Πλήθος)
    public class ProductReviewContainerDto
    {
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public IEnumerable<ReviewResponseDto> Reviews { get; set; } = new List<ReviewResponseDto>();
    }
}