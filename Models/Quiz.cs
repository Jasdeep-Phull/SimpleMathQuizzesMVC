﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DataAnnotationsExtensions;
using SimpleMathQuizzes.CustomValidation;


namespace SimpleMathQuizzes.Models
{
    /// <summary>
    /// A class representing a quiz in this app
    /// </summary>
    [ScoreIsLessThanOrEqualToNumberOfQuestions("Score", "Questions", ErrorMessage = "The score cannot be greater than the number of questions.")]
    public class Quiz : QuestionsAndAnswers
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "The date and time of creation was not automatically populated.")]
        [Display(Name = "Date and time of creation")]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy hh:mm:ss}")]
        public DateTimeOffset CreationDateTime { get; set; }
        // using DateTimeOffset instead of Date because the Postgresql database column for CreationDateTime uses the "Time with Time Stamp" data type

        [Required(ErrorMessage = "The score was not automatically populated.")]
        [Min(0, ErrorMessage = "The score cannot be below 0")]
        /* Min() is from the DataAnnotationsExtensions NuGet package,
         * the compiler give warning NU1701:

            * Package 'DataAnnotationsExtensions 5.0.1.27' was restored using '.NETFramework,Version=v4.6.1, .NETFramework,Version=v4.6.2, .NETFramework,Version=v4.7, .NETFramework,Version=v4.7.1, .NETFramework,Version=v4.7.2, .NETFramework,Version=v4.8, .NETFramework,Version=v4.8.1' instead of the project target framework 'net8.0'. This package may not be fully compatible with your project.

         * This data annotation has been tested, and it is currently (27/03/2024) working as intended
         */
        public int Score { get; set; }

        // Navigational property
        public string UserId { get; set; }
        [ForeignKey(nameof(UserId))]
        [InverseProperty(nameof(User.Quizzes))]
        public virtual User User { get; set; }



        /*
        see all ef core tables in PgAdmin4:
            select * from pg_catalog.pg_tables where schemaname='public';

        Migration commands:
            Add-Migration [Name]
            Update-Database

        Re-create certificates (in CLI): 
            dotnet dev-certs https --clean
            dotnet dev-certs https --trust
            dotnet dev-certs https --check
        */
    }
}
