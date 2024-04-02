using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations; // currently not used, but might be useful to keep
using System.ComponentModel.DataAnnotations.Schema;

namespace SimpleMathQuizzes.Models
{
    /// <summary>
    /// Class representing users.<br/>
    /// Inherits from IdentityUser, and adds the User.Quizzes navigational property.
    /// </summary>
    public class User : IdentityUser
    {
        /// <summary>
        /// Constructor for the User class.<br/>
        /// This initialises the quizzes property as an empty list, to avoid NullReferenceExceptions when accessing user.Quizzes.
        /// </summary>
        public User() : base()
        {
            // initialise quizzes as an empty list, to avoid NullReferenceExceptions when accessing user.Quizzes
            Quizzes = [];
        }

        [InverseProperty(nameof(Quiz.User))]
        public virtual IList<Quiz> Quizzes { get; set; }
    }
}
