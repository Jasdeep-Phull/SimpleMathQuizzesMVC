using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace SimpleMathQuizzes.CustomValidation
{
    /// <summary>
    /// Custom validation to ensure that score cannot be greater than the number of questions.<br/>
    /// Score is automatically calculated, so this validation blocks score calculation errors from entering the database.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class ScoreIsLessThanOrEqualToNumberOfQuestions : ValidationAttribute
    {
        private readonly string _propertyToValidate;
        private readonly string _propertyToCheck;
        
        /// <summary>
        /// Constructor for this custom validation.
        /// </summary>
        /// 
        /// <param name="propertyToValidate">
        /// the property to validate (score)
        /// </param>
        /// 
        /// <param name="propertyToCheck">
        /// the property to check with to validate the propertyToValidate (questions)
        /// </param>
        public ScoreIsLessThanOrEqualToNumberOfQuestions(string propertyToValidate, string propertyToCheck)
        {
            _propertyToValidate = propertyToValidate;
            _propertyToCheck = propertyToCheck;
        }

        public override bool IsValid(object? value)
        {
            try
            {
                PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(value);

                object? objectToValidate = properties.Find(_propertyToValidate, true).GetValue(value); // get score
                object? objectToCheck = properties.Find(_propertyToCheck, true).GetValue(value); // get questions

                int valueToValidate = (int)objectToValidate; // cast score to int
                int valueToCheck = ((List<string>)objectToCheck).Count; // cast questions to List<string>

                return (valueToValidate <= valueToCheck);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception occurred in score custom validation\nException: {ex}");
                // being unable to retrieve score or questions for validation is a serious problem, so this error is thrown
                throw;
            }
        }
    }
}


