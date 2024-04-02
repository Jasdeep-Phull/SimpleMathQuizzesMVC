using System.ComponentModel.DataAnnotations;

namespace SimpleMathQuizzes.ViewModels
{
    /// <summary>
    /// ViewModel for the create page
    /// </summary>
    public class QuestionsCreateViewModel
    {
        public QuestionsCreateViewModel(IList<string> questions)
        {
            Questions = questions;
        }

        [Required]
        public IList<string> Questions { get; set; }
    }
}
