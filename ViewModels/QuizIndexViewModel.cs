using SimpleMathQuizzes.Models;
using System.ComponentModel.DataAnnotations;

namespace SimpleMathQuizzes.ViewModels
{
    /// <summary>
    /// A quiz, with its score as a percentage.<br/>
    /// this is used as the ViewModel for the index page.
    /// </summary>
    public class QuizIndexViewModel
    {
        public QuizIndexViewModel(Quiz quiz, double scorePercentage)
        {
            Quiz = quiz;
            ScorePercentage = scorePercentage;
        }

        [Required]
        public Quiz Quiz { get; set; }

        [Required]
        [Display(Name = "Score (%)")]
        public double ScorePercentage { get; set; }
    }
}
