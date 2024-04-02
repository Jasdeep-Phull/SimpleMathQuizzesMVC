using SimpleMathQuizzes.Models;
using System.ComponentModel.DataAnnotations;

namespace SimpleMathQuizzes.ViewModels
{
    /// <summary>
    /// A quiz, with a list of correct answers for its questions.<br/>
    /// This is used in the details and edit pages.
    /// </summary>
    public class QuizWithAnswersViewModel
    {
        public QuizWithAnswersViewModel(Quiz quiz, IList<int> correctAnswers)
        {
            Quiz = quiz;
            CorrectAnswers = correctAnswers;
        }
        
        [Required]
        public Quiz Quiz { get; set; }

        [Required(ErrorMessage = "The list of correct answers was not automatically populated")]
        [Display(Name = "Correct Answers")]
        public IList<int> CorrectAnswers { get; set; }
       
    }
}
