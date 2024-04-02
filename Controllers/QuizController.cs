using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SimpleMathQuizzes.Data;
using SimpleMathQuizzes.Models;
using Microsoft.AspNetCore.Identity;
using SimpleMathQuizzes.ViewModels;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Security.Claims;
using Microsoft.Net.Http.Headers;
using System.Runtime.ExceptionServices;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using SimpleMathQuizzes.ExtraModels;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Authorization;
using SQLitePCL;
using Microsoft.Extensions.Logging;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using System.Security.Principal;
using Microsoft.IdentityModel.Tokens;
using Humanizer;
using System.Security.Policy;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace SimpleMathQuizzes.Controllers
{
    /// <summary>
    /// The controller responsible for handling all quiz-related requests.<br/>
    /// Has CRUD functions for quizzes, and some helper methods.
    /// </summary>
    public class QuizController : Controller
    {
        private readonly IAuthorizationService _authorizationService;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly ILogger _logger;

        // minimum and maximum values allowed for an answer
        private const int MIN_ANSWER_VALUE = -500;
        private const int MAX_ANSWER_VALUE = 500;


        /// <summary>
        /// Constructor for the QuizController.
        /// </summary>
        /// 
        /// <param name="authorizationService">
        /// The authorization service that only allows authorized users access a quiz.<br/>
        /// The authorization service uses the resource based authorization in QuizCustomAuthorization.cs to determine if the user can access a quiz.
        /// </param>
        /// 
        /// <param name="context">
        /// The EF Core DbContext, which is used to query, create, update and delete data in the Postgresql database of this website.
        /// </param>
        /// 
        /// <param name="userManager">
        /// The UserManager class provided by Identity, which is used to retreive information from the database about the current user.
        /// </param>
        /// 
        /// <param name="logger">
        /// A logger, to log information.
        /// </param>
        public QuizController(IAuthorizationService authorizationService, ApplicationDbContext context, UserManager<User> userManager, ILogger<QuizController> logger)
        {
            _authorizationService = authorizationService;
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }


        // GET: Quiz
        /// <summary>
        /// Retreives all of the current user's quizzes, by querying the DbContext.<br/>
        /// Calculates the score, as a percentage, for all quizzes.
        /// </summary>
        /// 
        /// <param name="sortBy">
        /// This is used to sort the quizzes.<br/>
        /// "score%_Ascending": Order by score, ascending.<br/>
        /// "score%_Descending": Order by score, descending.<br/>
        /// "dateTime_Ascending": Order by creation date and time, ascending.<br/>
        /// default ("dateTime_Descending"): Order by creation date and time, descending.
        /// </param>
        /// 
        /// <returns>
        /// Returns the Index View, with a list of quizzes and their score as a percentage, as a List of QuizIndexViewModel objects.<br/>
        /// Returns an Unauthorized result if the current user's ID cannot be found in the HttpContext.
        /// </returns>
        /// 
        /// <exception cref="Exception">
        /// This method will throw an exception if it cannot calcualte the score percentage of a quiz.
        /// </exception>
        public async Task<IActionResult> Index(string sortBy = "dateTime_Descending")
        {
            _logger.LogInformation("\nIndex Called");

            // set the hyperlinks for sorting the columns on the front end
            ViewData["DateTimeLinkParameter"] = sortBy.Equals("dateTime_Descending") ? "dateTime_Ascending" : "dateTime_Descending";
            ViewData["score%LinkParameter"] = sortBy.Equals("score%_Descending") ? "score%_Ascending" : "score%_Descending";


            string? userId = GetCurrentUserId();
            if (userId is null)
            {
                _logger.LogInformation("Unknown/Unauthenticated user has requested to view quizzes (Index Page))");
                return Unauthorized();
            }


            // try to find the quiz in the DbContext
            IList<Quiz> userQuizzes = await _context.Quizzes
                .AsNoTracking()
                .Where(q => q.UserId == userId)
                .ToListAsync();
            /* 
            ArgumentException: Expression of type 'System.Collections.Generic.ICollection`1[System.String]' cannot be used for parameter of type 'System.Collections.Generic.IList`1[System.String]' of method 'System.Collections.Generic.IList`1[System.String] PopulateList[String](System.Collections.Generic.IList`1[System.String], System.Collections.Generic.IList`1[System.String])' (Parameter 'arg0')
            
            Error: Collection<string> cannot be used for parameter IList<string> of IList.PopulateList(List<string>, List<string>), arg 0

            Because of this error I changed the models to store the questions and userAnswers as ILists instead of ICollections
            This solved the error
            */


            IList<QuizIndexViewModel> quizzesWithPercentages = [];
            if (userQuizzes.IsNullOrEmpty()) 
            {
                _logger.LogInformation("User(ID: {userId}) has requested to view their quizzes, could not find any quizzes for user", userId);
                return View(quizzesWithPercentages);
            }


            // create and populate the view model
            foreach (Quiz quiz in userQuizzes)
            {
                double scorePercentage = -1;
                try
                {
                    scorePercentage = (double)quiz.Score / quiz.Questions.Count * 100;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.StackTrace st = new();
                    _logger.LogInformation("Stack Trace: {st}", st);
                    string errorMessage = $"Error while calculating the score percentage of quiz {quiz.Id}. Score: {quiz.Score}, number of questions: {quiz.Questions.Count}";
                    _logger.LogCritical(errorMessage);
                    throw new Exception(errorMessage, ex);
                }
                quizzesWithPercentages.Add(new QuizIndexViewModel(quiz, scorePercentage));
            }

            /*
            this is more concise, but i dont know how i would log which specific quiz (and its score and number of questions) caused an exception

            try
            {
                IList<QuizIndexViewModel> userQuizzesWithPercentages2 = userQuizzes
                    .Select(quiz =>
                        new QuizIndexViewModel(quiz, (double)quiz.Score / quiz.Questions.Count * 100))
                    .ToList();
            }
            catch (Exception ex)
            {
                throw;
            }
            */


            // sort the view model according to the "sortBy" parameter
            quizzesWithPercentages = sortBy switch
            {
                "score%_Ascending" => quizzesWithPercentages.OrderBy(q => q.ScorePercentage).ToList(),
                "score%_Descending" => quizzesWithPercentages.OrderByDescending(q => q.ScorePercentage).ToList(),
                "dateTime_Ascending" => quizzesWithPercentages.OrderBy(q => q.Quiz.CreationDateTime).ToList(),
                // case: dateTime_Descending
                _ => quizzesWithPercentages.OrderByDescending(q => q.Quiz.CreationDateTime).ToList(),
            };


            _logger.LogInformation("User (ID: {userId}) has requested to view their quizzes, number of quizzes found: {quizzesWithPercentages.Count}", userId, quizzesWithPercentages.Count);

            return View(quizzesWithPercentages);
        }


        // GET: Quiz/Details/5
        /// <summary>
        /// Calls the GetQuizWithAnswers helper method, which queries the DbContext for the quiz, and then calculates the correct answers for the quiz.
        /// </summary>
        /// 
        /// <param name="id">
        /// The id of the quiz to look for.
        /// </param>
        /// 
        /// <returns>
        /// Returns the Details View, with the quiz and its correct answers as a QuizWithAnswers View Model.<br/>
        /// Returns a NotFound result if the quiz cannot be found.<br/>
        /// Returns a Forbid result if the user is unauthorized to access the quiz.<br/>
        /// Returns a Challenge result if the user is unauthenticated.
        /// </returns>
        public async Task<IActionResult> Details(int? id)
        {
            _logger.LogInformation("\nDetails Called");
            IActionResult action = await GetQuizWithAnswers(id, "view");
            return action;
        }


        // GET: Quiz/Create
        /// <summary>
        /// Generates a set of random questions, to send to the Create View.<br/>
        /// Also passes the MIN_ANSWER_VALUE and MAX_ANSWER_VALUE variables to the view via ViewData.
        /// </summary>
        /// 
        /// <returns>
        /// The Create View, with a list of questions as a QuestionsCreate View Model.
        /// </returns>
        public IActionResult Create()
        {
            _logger.LogInformation("\nCreate Called");

            int numberOfQuestions = 10;

            ViewData["MinAnswerValue"] = MIN_ANSWER_VALUE;
            ViewData["MaxAnswerValue"] = MAX_ANSWER_VALUE;


            QuestionsCreateViewModel questionsViewModel = new(GenerateQuestions(numberOfQuestions));

            _logger.LogInformation("User (ID: {GetCurrentUserId()}) has requested to create a new quiz", GetCurrentUserId());

            return View(questionsViewModel);
        }


        /* example method that uses TryValidateModel
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] string data)
        {
            var model = JsonConvert.DeserializeObject<ReceiveMoneyTransferModel>(data);
            if (!this.TryValidateModel(model))
            {
                return this.BadRequest();
            }

            var account =
                await this.bankAccountService.GetByUniqueIdAsync<BankAccountConciseServiceModel>(
                    model.DestinationBankAccountUniqueId);
            if (account == null || !string.Equals(account.UserFullName, model.RecipientName,
                StringComparison.InvariantCulture))
            {
                return this.BadRequest();
            }

            var serviceModel = new MoneyTransferCreateServiceModel
            {
                AccountId = account.Id,
                Amount = model.Amount,
                Description = model.Description,
                DestinationBankAccountUniqueId = model.DestinationBankAccountUniqueId,
                Source = model.SenderAccountUniqueId,
                SenderName = model.SenderName,
                RecipientName = model.RecipientName,
                ReferenceNumber = model.ReferenceNumber
            };

            var isSuccessful = await this.moneyTransferService.CreateMoneyTransferAsync(serviceModel);
            if (!isSuccessful)
            {
                return this.NoContent();
            }

            return this.Ok();
        }
        */


        // POST: Quiz/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        /// <summary>
        /// Validates the create data.<br/>
        /// Creates a new quiz, calculates the score of the quiz and populates all other quiz fields.<br/>
        /// Saves the changes to the DbContext.<br/>
        /// <br/>
        /// (Currently this method/endpoint is called via an AJAX request from the create page)
        /// </summary>
        /// 
        /// <param name="createData">
        /// The questions and answers to create a quiz from.<br/>
        /// this data is received as a QuestionsAndAnswers object through Model Binding.
        /// </param>
        /// 
        /// <returns>
        /// Returns a HTTP 201 JSON response if successful.<br/>
        /// Returns a JSON response with status code greater than or equal to 400 if any problems or errors are encountered during the request.
        /// </returns>
        [Produces("application/json")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromBody] QuestionsAndAnswers createData)
        {
            /* The React + Web API project uses problem details responses
             * I kept the JSON responses of this website simple, because the Create and Edit POST methods are only meant to be called by the Create and Edit Views, respectively
            */

            _logger.LogInformation("\nCreate Post Called");

            // check if request body is null
            if (createData is null)
            {
                string errorMessage = "Unable to create quiz: data for new quiz deserialised from JSON request was null";
                _logger.LogError(errorMessage);

                return BadRequest(new
                {
                    success = false,
                    statusCode = StatusCodes.Status400BadRequest,
                    /* the 2 above properties of these responses are redunant, but i already added it to all the responses, so i chose to leave it in the response body
                     * i was unsure how to manually check if the jqXHR object responses from Jquery AJAX requests were successful, and how to get the status code from them, so i added them to the response body
                     */
                    message = errorMessage
                });
            }
            

            // check if data is valid
            if (!TryValidateModel(createData))
            {
                string errorMessage = "Unable to create quiz: data for new quiz was invalid";
                _logger.LogInformation(errorMessage);

                // get errors from ModelState, and put them in one string, separated with newline characters ("\n")
                string errors = LogError(ModelState);
                _logger.LogInformation($"Errors: {errors}");

                return BadRequest(new
                {
                    success = false,
                    statusCode = StatusCodes.Status400BadRequest,
                    message = $"{errorMessage}\nErrors: {errors}", // i am now aware that returning a ValidationProblem response automatically does what i have manually done here. i have already implemented this so i decided to leave it in
                });
            }
            _logger.LogInformation("CreateData is valid");


            // try to calculate the score of the quiz
            int score = -1;
            try
            {
                score = CalculateScore(createData.Questions, createData.UserAnswers);
            }
            catch (Exception ex)
            {
                string errorMessage = "Unable to create quiz: unexpected error encountered when calculating the score and correct answers of the quiz";
                _logger.LogError(errorMessage);
                _logger.LogInformation("Exception: {ex}", ex);

                return BadRequest(new
                {
                    success = false,
                    statusCode = StatusCodes.Status400BadRequest,
                    message = errorMessage,
                });
            }
            _logger.LogInformation("Score calculated successfully");


            string? userId = GetCurrentUserId();
            // check if userId is null (couldn't find the current user's Id in the HttpContext)
            if (userId is null)
            {
                string errorMessage = "Unable to create quiz: unable to retrieve the current user's ID";
                _logger.LogError(errorMessage);

                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    success = false,
                    statusCode = StatusCodes.Status403Forbidden,
                    message = errorMessage,
                });
            }
            _logger.LogInformation("UserId successfully determined");

            // find user in userManager
            User? user = await _userManager.Users
                .Include(u => u.Quizzes)
                .FirstOrDefaultAsync(u => u.Id == userId);

            // check if user is null (couldn't find the current user's information in the database, using their Id)
            if (user is null)
            {
                string errorMessage = "Unable to create quiz: unable to retrieve the current user's data using the current user's ID";
                _logger.LogError(errorMessage);

                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    success = false,
                    statusCode = StatusCodes.Status500InternalServerError,
                    message = errorMessage,
                });
            }
            _logger.LogInformation("User successfully retrieved from ID");


            // create a new quiz object
            Quiz quiz = new()
            {
                // quiz ID is automatically generated
                CreationDateTime = DateTimeOffset.Now, // Postgresql database uses the "timestamp with time zone" datatype for CreationDateTime, so DateTimeOffset is needed
                Questions = createData.Questions,
                UserAnswers = createData.UserAnswers,
                Score = score,
                UserId = userId,
                User = user // the method "user.Quizzes.Add(quiz)" below this might auto assign this, but i manually assign it just in case it doesn't
            };
            
            /* adding the quiz to the user's quizzes before validating the quiz
             * this isn't a problem because "_context.SaveChangesAsync()" is called only if the quiz is valid
             * so if the quiz was not valid then this change wouldn't be saved
             */
            user.Quizzes.Add(quiz);
            _logger.LogInformation($"  User quizzes: {string.Join(", ", user.Quizzes)}");


            _logger.LogInformation("Quiz created");

            // id is 0 here, but it will be correctly generated when "_context.SaveChangesAsync()" is successful
            // _logger.LogInformation($"Id: {quiz.Id}");
            _logger.LogInformation($"CreationDateTime: {quiz.CreationDateTime}");
            _logger.LogInformation($"Questions: {string.Join(", ", quiz.Questions)}");
            _logger.LogInformation($"Answers: {string.Join(", ", quiz.UserAnswers)}");
            _logger.LogInformation($"Score: {quiz.Score}");
            _logger.LogInformation($"UserId: {quiz.UserId}");
            _logger.LogInformation($"User: {quiz.User.Id}, {quiz.User}");


            /* check if newly created quiz is valid
             * this is mainly to check that the automatically populated fields (Score, CreationDateTime, UserId and User) are valid
             * the questions and answers are already valid, because they passed the 1st validation check in this method
             */
            if (!TryValidateModel(quiz))
            {
                string errorMessage = "Unable to create quiz: unexpected error encountered when creating quiz and populating automatically computed fields";
                _logger.LogError(errorMessage);

                // get errors from ModelState, and put them in one string, separated with newline characters ("\n")
                string errors = LogError(ModelState);
                _logger.LogInformation($"Errors: {errors}");

                /* return a HTTP 500 response for this validation error, since the fields that fail validation here were automatically populated by the controller, not the user
                 * if Questions or UserAnswers were invalid, they would fail the 1st validation check in this method
                 */
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    success = false,
                    statusCode = StatusCodes.Status500InternalServerError,
                    message = $"{errorMessage}\nErrors: {errors}"
                });
            }
            _logger.LogInformation("Quiz is valid");


            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                string errorMessage = "Unable to create quiz: unexpected error encountered when saving new quiz to database";
                _logger.LogError(errorMessage);
                _logger.LogInformation("Exception: {ex}", ex);

                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    success = false,
                    statusCode = StatusCodes.Status500InternalServerError,
                    message = errorMessage
                });
            }
            // if "saveChangesAsync()" did not throw an exception, then it was executed successfully

            _logger.LogInformation($"user.Quizzes after create: {string.Join(", ", user.Quizzes)}");
            if (user.Quizzes is null) { _logger.LogInformation("user.Quizzes is null"); }

            string successMessage = "Successfully created new quiz (and updated database)";
            _logger.LogInformation(successMessage);


            return StatusCode(StatusCodes.Status201Created, new
            {
                success = true,
                statusCode = StatusCodes.Status201Created,
                message = successMessage
            });
        }


        // GET: Quiz/Edit/5
        /// <summary>
        /// Queries the DbContext for a quiz, and checks if the current user has authorization to access it.<br/>
        /// Also passes the MIN_ANSWER_VALUE and MAX_ANSWER_VALUE variables to the view via ViewData.
        /// </summary>
        /// 
        /// <param name="id">
        /// The id of the quiz to look for.
        /// </param>
        /// 
        /// <returns>
        /// Returns the Edit View, with the retrieved quiz as the Model.<br/>
        /// Returns a NotFound result if the quiz cannot be found.<br/>
        /// Returns a Forbid result if the user is unauthorized to access the quiz.<br/>
        /// Returns a Challenge result if the user is unauthenticated.
        /// </returns>
        public async Task<IActionResult> Edit(int? id)
        {
            _logger.LogInformation("\nEdit Called");

            ViewData["MinAnswerValue"] = MIN_ANSWER_VALUE;
            ViewData["MaxAnswerValue"] = MAX_ANSWER_VALUE;


            if (id is null) { return NotFound(); }


            Quiz? quiz = await _context.Quizzes.FindAsync(id);
            if (quiz is null) { return NotFound(); }


            string? userId = GetCurrentUserId();
            
            // check if user can access this quiz
            AuthorizationResult authorizationResult = await _authorizationService.AuthorizeAsync(HttpContext.User, quiz, "CanAccessQuiz");
            if (authorizationResult.Succeeded)
            {
                _logger.LogInformation("Authorised user (ID: {userId}) has requested to edit a quiz (ID: {id})",userId, id);
                return View(quiz);
            }
            else if (HttpContext.User.Identity?.IsAuthenticated == true)
            /* "== true" is needed in the above statement. if it isn't there then the compiler will throw CS0266: cannot convert bool? to bool
             * this can be solved by casting to bool, but casting makes things unnecessarily messy, because then you need a "catch" block to catch a cast error
             */
            {
                _logger.LogInformation("Unauthorised user (ID: {userId}) has requested to edit a quiz (ID: {id})", userId, id);
                return new ForbidResult();
            }
            else
            {
                _logger.LogInformation("Unauthenticated user has requested to edit a quiz (ID: {id})", id);
                return new ChallengeResult();
            }
        }


        // POST: Quiz/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        /// <summary>
        /// Validates the update data.<br/>
        /// Re-calculates the score of the quiz.<br/>
        /// Saves the changes to the DbContext.<br/>
        /// <br/>
        /// (Currently this method/endpoint is called via an AJAX request from the edit page)
        /// </summary>
        /// 
        /// <param name="updateData">
        /// The id of the quiz, to find it in the DbContext, and the new answers to update the quiz with.<br/>
        /// This data is received as a ReceiveUpdatedUserAnswersModel object through Model Binding.
        /// </param>
        /// 
        /// <returns>
        /// Returns a HTTP 200 JSON response if successful.<br/>
        /// Returns a JSON response with status code greater than or equal to 400 if any problems or errors are encountered during the request.
        /// </returns>
        [Produces("application/json")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([FromBody] ReceiveUpdatedUserAnswersModel updateData)
        {
            /* The React + Web API project uses problem details responses
             * I kept the JSON responses of this website simple, because the Create and Edit POST methods are only meant to be called by the Create and Edit Views, respectively
            */

            _logger.LogInformation("Edit Post Called");

            // check if the request body is null
            if (updateData is null)
            {
                string errorMessage = "Unable to update quiz: id and answers deserialised from JSON request were null";
                _logger.LogError(errorMessage);

                return BadRequest(new
                {
                    success = false,
                    statusCode = StatusCodes.Status400BadRequest,
                    /* the 2 above properties of these responses are redunant, but i already added it to all the responses, so i chose to leave it in the response body
                     * i was unsure how to manually check if the jqXHR object responses from Jquery AJAX requests were successful, and how to get the status code from them, so i added them to the response body
                     */
                    message = errorMessage
                });
            }
            _logger.LogInformation("updateData is not null");


            // check if the data is valid
            if (!TryValidateModel(updateData))
            {
                string errorMessage = "Unable to update quiz: answers were invalid";
                _logger.LogInformation(errorMessage);

                // get errors from ModelState, and put them in one string, separated with newline characters ("\n")
                string errors = LogError(ModelState);
                _logger.LogInformation($"Errors: {errors}");

                return BadRequest(new
                {
                    success = false,
                    statusCode = StatusCodes.Status400BadRequest,
                    message = $"{errorMessage}\nErrors: {errors}",
                });
            }
            _logger.LogInformation("updateData is valid");

            
            // find quiz in the DbContext, using the quiz id in the data
            Quiz? quiz = await _context.Quizzes
                .Include(q => q.User)
                .FirstOrDefaultAsync(q => q.Id == updateData.Id);

            // check if quiz is null (couldn't find the quiz in the context)
            if (quiz is null)
            {
                string errorMessage = "Unable to update quiz: quiz to update cannot be found";
                _logger.LogInformation(errorMessage);

                return NotFound(errorMessage);
            }
            _logger.LogInformation($"quiz to update found (ID: {updateData.Id})");
            _logger.LogInformation($"quiz user: {quiz.User})");


            string? userId = GetCurrentUserId();
            // authorization handler deals with userId == null

            // check if the current user is authorized to access this quiz
            AuthorizationResult authorizationResult = await _authorizationService.AuthorizeAsync(HttpContext.User, quiz, "CanAccessQuiz");
            if (!authorizationResult.Succeeded)
            {
                if (HttpContext.User.Identity?.IsAuthenticated == true)
                /* "== true" is needed in the above statement. if it isn't there then the compiler will throw CS0266: cannot convert bool? to bool
                 * this can be solved by casting to bool, but casting makes things unnecessarily messy, because then you need a "catch" block to catch a cast error
                 */
                {
                    _logger.LogInformation("Unauthorised user (ID: {userId}) has posted an edit request for a quiz (ID: {quiz.Id})", userId, quiz.Id);
                }
                else
                {
                    _logger.LogInformation("Unauthenticated user has requested access to edit quiz (ID: {quiz.Id})", quiz.Id);
                }

                return StatusCode(StatusCodes.Status403Forbidden);
            }
            _logger.LogInformation("Authorised user (ID: {userId}) has posted an edit request for a quiz (ID: {quiz.Id})", userId, quiz.Id);


            // try to re-calculate the score of the quiz
            int score = -1;
            try
            {
                score = CalculateScore(quiz.Questions, updateData.UserAnswers);
            }
            catch (Exception ex)
            {
                string errorMessage = "Unable to update quiz: unexpected error encountered when re-calculating the score of the quiz";
                _logger.LogError(errorMessage);
                _logger.LogInformation("Exception: {ex}", ex);

                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    success = false,
                    statusCode = StatusCodes.Status500InternalServerError,
                    message = errorMessage,
                });
            }
            _logger.LogInformation("score re-calculated successfully");
            
            
            /* update quiz user answers and score
             * saveChangesAsync() has not been called yet, so these changes will not be saved yet if there are any problems
             */
            quiz.Score = score;
            quiz.UserAnswers = updateData.UserAnswers;

            _logger.LogInformation("updated data assigned");


            /* check if the quiz is still valid after the changes have been made
             * this is mainly to check that the score calculated is valid
             * the new answers are already valid, because they passed the 1st validation check in this method
             */
            if (!TryValidateModel(quiz))
            {
                string errorMessage = "Unable to update quiz: unexpected error encountered when updating the quiz";
                _logger.LogError(errorMessage);

                // get errors from ModelState, and put them in one string, separated with newline characters ("\n")
                string errors = LogError(ModelState);
                _logger.LogInformation($"Errors: {errors}");

                /* return a HTTP 500 response for this validation error, since the fields that fail validation here were automatically populated by the controller, not the user
                 * if the new answers were invalid, they would fail the 1st validation check in this method
                 */
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    success = false,
                    statusCode = StatusCodes.Status500InternalServerError,
                    message = $"{errorMessage}\nErrors: {errors}"
                });
            }
            _logger.LogInformation("quiz is valid");
            
            
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (!QuizExists(quiz.Id))
                {
                    // quiz no longer exists in the database
                    string errorMessage = "Unable to update quiz: quiz to update does not exist";
                    _logger.LogInformation(errorMessage);

                    return NotFound(errorMessage);
                }
                else
                {
                    _logger.LogError("Unable to update quiz: DbUpdateConcurrencyException : {ex}", ex);
                    throw;
                }
            }
            catch (Exception ex)
            {
                string errorMessage = "Unable to update quiz: unexpected error encountered when saving changes to database";
                _logger.LogError(errorMessage);
                _logger.LogInformation("Exception: {ex}", ex);

                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    success = false,
                    statusCode = StatusCodes.Status500InternalServerError,
                    message = errorMessage
                });
            }
            // if "saveChangesAsync()" did not throw an exception, then it was executed successfully


            string successMessage = $"Succcessfully updated quiz (ID: {quiz.Id}) (and updated database)";
            _logger.LogInformation(successMessage);

            return Ok(new
            {
                success = true,
                statusCode = StatusCodes.Status200OK,
                message = successMessage
            });
        }


        // GET: Quiz/Delete/5
        /// <summary>
        /// Calls the GetQuizWithAnswers helper method, which queries the context for the quiz, and then calculates the correct answers for the quiz.
        /// </summary>
        /// 
        /// <param name="id">
        /// The id of the quiz to look for.
        /// </param>
        /// 
        /// <returns>
        /// Returns the Delete View, with the quiz and its correct answers as a QuizWithAnswers View Model.<br/>
        /// Returns a NotFound result if the quiz cannot be found.<br/>
        /// Returns a Forbid result if the user is unauthorized to access the quiz.<br/>
        /// Returns a Challenge result if the user is unauthenticated.
        /// </returns>
        public async Task<IActionResult> Delete(int? id)
        {
            _logger.LogInformation("Delete Called");
            IActionResult action = await GetQuizWithAnswers(id, "delete");
            return action;
        }


        // POST: Quiz/Delete/5
        /// <summary>
        /// Queries the context for the quiz.<br/>
        /// Removes the quiz from the DbContext.<br/>
        /// Saves the changes to the DbContext.
        /// </summary>
        /// 
        /// <param name="id">
        /// The id of the quiz to look for.
        /// </param>
        /// 
        /// <returns>
        /// Returns a redirect to the Index page if the delete request was successful.<br/>
        /// Returns a NotFound result if the quiz cannot be found.<br/>
        /// Returns a HTTP 500 response if the changes could not be saved to the DbContext.<br/>
        /// Returns a Forbid result if the user is unauthorized to access the quiz.<br/>
        /// Returns a Challenge result if the user is unauthenticated.
        /// </returns>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            _logger.LogInformation("Delete Post Called");

            // try to find the quiz in the context
            Quiz? quiz = await _context.Quizzes.FindAsync(id);
            if (quiz is null) { return NotFound(); }
            
            string? userId = GetCurrentUserId();
            // authorization handler deals with userId == null

            // check if the current user is authorized to access this quiz
            AuthorizationResult authorizationResult = await _authorizationService.AuthorizeAsync(HttpContext.User, quiz, "CanAccessQuiz");
            if (authorizationResult.Succeeded)
            {
                _logger.LogInformation("Authorised user (ID: {userId}) has posted a request to delete a quiz (ID: {id})", userId, id);
                // if the user has been authenticated then the id will not be null

                // find the user in the userManager
                // if the user has been authenticated then the user returned from this query will not be null
                User? user = await _userManager.Users
                    .Include(u => u.Quizzes)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                // remove this quiz from the user's quizzes
                user.Quizzes.Remove(quiz);

                /* also directly remove the quiz from the context
                 * the above line of code should automatically do this, but i left this here just in case it doesn't
                 */
                _context.Quizzes.Remove(quiz);

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    string errorMessage = "Unable to delete quiz: unexpected error encountered when saving changes to the database";
                    _logger.LogError(errorMessage);
                    _logger.LogInformation("Exception: {ex}", ex);
                    return StatusCode(StatusCodes.Status500InternalServerError);
                }
                // if "saveChangesAsync()" did not throw an exception, then it was executed successfully


                _logger.LogInformation($"user.Quizzes after delete: {string.Join(", ", user.Quizzes)}");
                if (user.Quizzes is null) { _logger.LogInformation("user.Quizzes is null"); }

                _logger.LogInformation("Successfully deleted quiz (ID: {id})", id);

                // redirect to index if successful
                return RedirectToAction(nameof(Index));
            }
            else if (HttpContext.User.Identity?.IsAuthenticated == true)
            /* "== true" is needed in the above statement. if it isn't there then the compiler will throw CS0266: cannot convert bool? to bool
             * this can be solved by casting to bool, but casting makes things unnecessarily messy, because then you need a "catch" block to catch a cast error
             */
            {
                _logger.LogInformation("Unauthorised user (ID: {userId}) has posted a request to delete a quiz (ID: {id})", userId, id);
                return new ForbidResult();
            }
            else
            {
                _logger.LogInformation("Unauthenticated user has posted a request to delete quiz (ID: {id})", id);
                return new ChallengeResult();
            }
        }



        // non endpoint methods (private helper methods):



        /// <summary>
        /// Checks if the quiz exists in the DbContext.
        /// </summary>
        /// 
        /// <param name="id">
        /// The id of the quiz to look for.
        /// </param>
        /// 
        /// <returns>
        /// Returns True if the quiz is in the DbContext.<br/>
        /// Returns False if the quiz cannot be found in the DbContext.
        /// </returns>
        private bool QuizExists(int id)
        {
            return _context.Quizzes.Any(q => q.Id == id);
        }

        /// <summary>
        /// Retrieves the current user's ID from the claims stored in the HttpContext.User ClaimsPrincipal.
        /// </summary>
        /// 
        /// <returns>
        /// Returns the current user's ID.<br/>
        /// Returns null if the current user's ID cannot be found in the HttpContext.User ClaimsPrincipal.
        /// </returns>
        private string? GetCurrentUserId()
        {
            string? id = null;

            if (HttpContext.User is null) { _logger.LogError("Current user is unauthenticated"); }
            else
            {
                id = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (id is null) { _logger.LogError("Current user is unauthenticated"); }
            }
            return id;
        }

        /// <summary>
        /// Formats all of the errors in the modelState parameter as one string, separated by new line characters ("\n").
        /// </summary>
        /// 
        /// <param name="modelState">
        /// The ModelStateDictionary to check for errors.
        /// </param>
        /// 
        /// <returns>
        /// A string with all of the errors from the modelState, separated by new line characters ("\n").
        /// </returns>
        private string LogError(ModelStateDictionary modelState)
        {
            _logger.LogInformation($"Number of errors: {modelState.ErrorCount}");

            IEnumerable<string?> modelErrors = modelState.Values
                .SelectMany(v => v.Errors)
                .Select(v => v.ErrorMessage);

            string errors = string.Join(", ", modelErrors);
            if (errors.Trim().Equals(",")) // using Trim() to remove all spaces at the start and end
            {
                return ""; // return an empty string if modelErrors is full of empty strings
            }
            else
            {
                return errors;
            }
        }
        

        /// <summary>
        /// Queries the context for the quiz, and then calculates the correct answers for the quiz.
        /// </summary>
        /// 
        /// <param name="id">
        /// The id of the quiz to look for.
        /// </param>
        /// 
        /// <param name="requestType">
        /// The type of view to return.<br/>
        /// "view": Returns the Details View.<br/>
        /// "delete": Returns the Delete View. 
        /// </param>
        /// 
        /// <returns>
        /// Returns the View specified by the requestType parameter, with the quiz and its correct answers as a QuizWithAnswers View Model.<br/>
        /// Returns a NotFound result if the quiz cannot be found.<br/>
        /// Returns a HTTP 500 response if the requestType parameter is not "view" or "delete".<br/>
        /// Returns a Forbid result if the user is unauthorized to access the quiz.<br/>
        /// Returns a Challenge result if the user is unauthenticated.
        /// </returns>
        private async Task<IActionResult> GetQuizWithAnswers(int? id, string requestType)
        {
            if (id is null) { return NotFound(); }

            Quiz? quiz = await _context.Quizzes.FindAsync(id); // the User navigational property has not been included in this query

            if (quiz is null) { return NotFound(); }
            _logger.LogInformation("Quiz found");

            _logger.LogInformation($"userId {HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)}");
            _logger.LogInformation($"Quiz UserId {quiz.UserId}");
            
            string? userId = GetCurrentUserId();
            
            // check if the current user is authorized to access this quiz
            AuthorizationResult authorizationResult = await _authorizationService.AuthorizeAsync(HttpContext.User, quiz, "CanAccessQuiz");
            if (authorizationResult.Succeeded)
            {
                _logger.LogInformation("Authorised user (ID: {userId}) has requested to {requestType} a quiz (ID: {id})", userId, requestType, id);
                IList<int> correctAnswers = CalculateAnswers(quiz.Questions);

                // return based on the value of the requestType parameter
                switch (requestType)
                {
                    case "view":
                        return View("Details", new QuizWithAnswersViewModel(quiz, correctAnswers));
                    case "delete":
                        return View("Delete", new QuizWithAnswersViewModel(quiz, correctAnswers));
                    default:
                        _logger.LogError("requestType passed to GetQuizWitAnswers() was not \"view\" or \"delete\"");
                        return StatusCode(StatusCodes.Status500InternalServerError);
                }
            }
            else if (HttpContext.User.Identity?.IsAuthenticated == true)
             /* "== true" is needed in the above statement. if it isn't there then the compiler will throw CS0266: cannot convert bool? to bool
              * this can be solved by casting to bool, but casting makes things unnecessarily messy, because then you need a "catch" block to catch a cast error
              */
            {
                _logger.LogInformation("Unauthorised user (ID: {userId}) has requested to {requestType} a quiz (ID: {id})", userId, requestType, id);
                _logger.LogInformation("Failed requirements: {authorizationResult.Failure.FailedRequirements}, reasons: {authorizationResult.Failure.FailureReasons}", authorizationResult.Failure.FailedRequirements, authorizationResult.Failure.FailureReasons);
                return new ForbidResult();
            }
            else
            {
                _logger.LogInformation("Unauthenticated user has requested to {requestType} quiz (ID: {id})", requestType, id);
                return new ChallengeResult();
            }
        }


        /// <summary>
        /// Generates a list of questions for a new quiz.<br/>
        /// Also tries to calculate the answer for each question before adding it the the list of questions.<br/>
        /// This avoids the possibility/problem of passing a question that cannot be evaluated to the user.<br/>
        /// <br/>
        /// Current range of values for each question:<br/>
        /// [1-100] + [1-100]<br/>
        /// [10-100] - [1-99]<br/>
        /// [2-10] * [2-20]
        /// </summary>
        /// 
        /// <param name="numberOfQuestions">
        /// The number of questions to generate.
        /// </param>
        /// 
        /// <returns>
        /// The generated questions, as a List of Strings.
        /// </returns>
        private IList<string> GenerateQuestions(int numberOfQuestions)
        {
            /*
            Range of values for each question:
                [1<->100] + [1<->100]
                [10<->100] - [1<->99]
                [2<->10] * [2<->20]
            */
    
            List<string> operators = ["+", "-", "*"];

            // limits for each value and operator
            const int ADD_MIN_VALUE = 1;
            const int ADD_MAX_VALUE = 100;

            const int SUBTRACT_MIN_FIRST_VALUE = 10;
            const int SUBTRACT_MAX_FIRST_VALUE = 100;
            const int SUBTRACT_MIN_SECOND_VALUE = 1;
            const int SUBTRACT_MAX_SECOND_VALUE = 99;

            const int MULTIPLY_MIN_VALUE = 2;
            const int MULTIPLY_MAX_FIRST_VALUE = 10;
            const int MULTIPLY_MAX_SECOND_VALUE = 20;

            Random rand = new();

            IList<string> questions = [];
            for (int i = 0; i < numberOfQuestions; i++)
            {
                string question = "";

                bool unique = false;
                while (unique is false)
                {
                    // generate question
                    int operatorIndex = rand.Next(operators.Count);
                    string questionOperator = operators[operatorIndex];
                    int firstNumber, secondNumber;

                    switch (questionOperator)
                    {
                        case "+":
                            firstNumber = rand.Next(ADD_MIN_VALUE, ADD_MAX_VALUE);
                            secondNumber = rand.Next(ADD_MIN_VALUE, ADD_MAX_VALUE);
                            break;
                        case "-":
                            firstNumber = rand.Next(SUBTRACT_MIN_FIRST_VALUE, SUBTRACT_MAX_FIRST_VALUE);
                            secondNumber = rand.Next(SUBTRACT_MIN_SECOND_VALUE, SUBTRACT_MAX_SECOND_VALUE);
                            break;
                        case "*":
                            firstNumber = rand.Next(MULTIPLY_MIN_VALUE, MULTIPLY_MAX_FIRST_VALUE);
                            secondNumber = rand.Next(MULTIPLY_MIN_VALUE, MULTIPLY_MAX_SECOND_VALUE);
                            break;
                        default:
                            _logger.LogError("Question operator ({questionOperator}) is not +, - or *", questionOperator);
                            continue;
                    }

                    question = firstNumber.ToString() + questionOperator + secondNumber.ToString();

                    // check if this question has already been generated, and start a new iteration if it has
                    if (questions.Contains(question)) { continue; }

                    /* check if question can be evaluated before it is sent to the view
                     * EvaluateQuestion() is called with throwException = false, because there is no need to terminate the program if there is an error here. Instead the question will be re-generated, and the error will be logged inside EvaluateQuestion()
                     */
                    int? answer = EvaluateQuestionNullable(question, false);

                    // check if the question was successfully evaluated, and start a new iteration if it wasn't
                    if (answer is null) { continue; }
                    // EvaluateQuestionNullable() handles logging for questions that cannot be evaluated
                    _logger.LogInformation($"{question}, answer: {answer}");

                    unique = true;
                }
                // add question to return list if it is unique and can be evaluated
                questions.Add(question);
            }

            string questionsString = string.Join(", ", questions);
            _logger.LogInformation("List of questions returned from GenerateQuestions({numberOfQuestions}): {questionsString}", numberOfQuestions, questionsString);

            return questions;
        }


        /// <summary>
        /// Calculates the score of a quiz.<br/>
        /// This is done by comparing the correct answer of each question with the corressponding user answer, and incrementing the score if they are the same
        /// </summary>
        /// 
        /// <param name="questions">
        /// The questions of the quiz
        /// </param>
        /// 
        /// <param name="userAnswers">
        /// The user answers of the quiz
        /// </param>
        /// 
        /// <returns>
        /// The score of the quiz
        /// </returns>
        private int CalculateScore(IList<string> questions, IList<int?> userAnswers)
        {
            // Calculate the correct answers for each question, and store them in a list
            IList<int> correctAnswers = CalculateAnswers(questions);
            int score = 0;

            for (int i = 0; i < questions.Count; i++)
            {
                int? userAnswer = userAnswers.ElementAt(i);
                int correctAnswer = correctAnswers.ElementAt(i);

                if (userAnswer == correctAnswer)
                {
                    score++;
                    _logger.LogInformation("UserAnswer: {userAnswer}, CorrectAnswer: {correctAnswer}. Answer matches, score incremented", userAnswer, correctAnswer);
                }
                else
                {
                    _logger.LogInformation("UserAnswer: {userAnswer}, CorrectAnswer: {correctAnswer}. Answer does not match, score not incremented", userAnswer, correctAnswer);
                }
            }
            _logger.LogInformation("Score returned: {score}", score);
            return score;
        }


        /// <summary>
        /// Calculates the correct answers for a list of questions.
        /// </summary>
        /// 
        /// <param name="questions">
        /// The questions to calculate answers for
        /// </param>
        /// 
        /// <returns>
        /// The correct answers for the questions supplied, as a List of Integers
        /// </returns>
        private IList<int> CalculateAnswers(IList<string> questions)
        {
            IList<int> correctAnswers = [];

            foreach (string question in questions)
            {
                int correctAnswer = EvaluateQuestion(question);
                correctAnswers.Add(correctAnswer);
            }
            return correctAnswers;


            /* This is a more concise version of the above code with the same functionality. It is used in the API project
            
            return questions
                .Select(EvaluateQuestion)
                .ToList();
            */
        }


        /// <summary>
        /// Calculates the answer to the question supplied, by using the DataTable.Compute() method.<br/>
        /// This method will throw any exceptions it encounters.<br/>
        /// This method calls EvaluateQuestionNullable() with parameter throwException = true, and then casts the result from int? to int
        /// </summary>
        /// 
        /// <param name="question">
        /// The question to calculate an answer for.
        /// </param>
        /// 
        /// <returns>
        /// The correct answer for the question, as an Integer
        /// </returns>
        /// 
        /// <exception cref="InvalidOperationException">
        /// This exception is thrown if the result of EvaluateQuestionNullable(question, true) cannot be cast to int.<br/>
        /// As far as I can see, this can only happen if EvaluteQuestionNullable(question, true) returns null (despite being called with throwException = true)
        /// </exception>
        /// 
        /// <exception cref="Exception">
        /// This exception is thrown if any error except InvalidOperationException is encountered when casting from int? to int.<br/>
        /// (This is not thrown if there are errors from EvaluateQuestionNullable(), because EvaluateQuestionNullable() handles its own errors/exceptions)
        /// </exception>
        private int EvaluateQuestion(string question)
        {
            try
            {
                int answer = (int)EvaluateQuestionNullable(question, true); // parameter throwException is set to True by default, but I left it here because it is very important that it is true for this method
                return answer;
            }
            catch (InvalidOperationException ex)
            {
                string errorMessage = $"EvaluteQuestionNullable({question}, true) returned null";
                _logger.LogCritical(errorMessage);
                throw new InvalidOperationException(errorMessage, ex);
            }
            catch (Exception ex)
            {
                string errorMessage = $"Unknown error when casting int? to int: {question}";
                _logger.LogCritical(errorMessage);
                throw new Exception(errorMessage, ex);
            }
        }


        /// <summary>
        /// Calculates the answer to the question supplied, by using the DataTable.Compute() method.<br/>
        /// The throwException parameter determines how this method deals with errors/exceptions.<br/>
        /// <br/>
        /// Having a version of this method that does not throw exceptions is useful when pre-evaluating generated questions. Please check the comments in generateQuestions() for more details.
        /// </summary>
        /// 
        /// <param name="question">
        /// The question to calculate an answer for.
        /// </param>
        /// 
        /// <param name="throwException">
        /// Determines what to do when errors are encountered.<br/>
        /// if this is "True" then any exceptions encountered will be thrown.<br/>
        /// if this is "False" then any exceptions encountered will be suppressed.
        /// </param>
        /// 
        /// <returns>
        /// Returns the correct answer for the question, as an Integer.<br/>
        /// Returns null if any errors were encountered and throwException = false.
        /// </returns>
        private int? EvaluateQuestionNullable(string question, bool throwException = true)
        {
            // the value to return if throwException is false
            int? notThrownExceptionReturnValue = null;

            try
            {
                int correctAnswer = Convert.ToInt32(new DataTable().Compute(question, null));
                return correctAnswer;
            }
            catch (ArgumentNullException ex) // question was null
            {
                int? returnValue = (int?)HandleException(
                    $"Question is null",
                    ExceptionDispatchInfo.Capture(ex),
                    throwException,
                    notThrownExceptionReturnValue);
                return returnValue;
            }
            catch (EvaluateException ex) // the Datatable could not compute the question
            {
                int? returnValue = (int?)HandleException(
                    $"Unable to evaluate question: {question}",
                    ExceptionDispatchInfo.Capture(ex),
                    throwException,
                    notThrownExceptionReturnValue);
                return returnValue;
            }
            catch (FormatException ex) // The result of DataTable.Compute() could not be converted to an Integer
            {
                int? returnValue = (int?)HandleException(
                    $"Computed answer cannot be converted to int: {new DataTable().Compute(question, null)}",
                    ExceptionDispatchInfo.Capture(ex),
                    throwException,
                    notThrownExceptionReturnValue);
                return returnValue;
            }
            catch (Exception ex) // an unknown exception occured when evaluating the question
            {
                int? returnValue = (int?)HandleException(
                    $"Unknown error when evaluating question: {question}",
                    ExceptionDispatchInfo.Capture(ex),
                    throwException,
                    notThrownExceptionReturnValue);
                return returnValue;

            }
        }


        /// <summary>
        /// If the parameter throwException = true, then throw the exception = true, then throw the exception stored in the ExceptionDispatchInfo.<br/>
        /// If the parameter throwException = false, return the parameter returnValue.<br/>
        /// <br/>
        /// This method's main purpose is to avoid code repitition.
        /// </summary>
        /// 
        /// <param name="errorMessage">
        /// The error message to log
        /// </param>
        /// 
        /// <param name="ex">
        /// The ExceptionDispatchInfo object, with the exception stored inside it
        /// </param>
        /// 
        /// <param name="throwException">
        /// Determines what to do when errors are encountered.<br/>
        /// if this is "True" then any exceptions encountered will be thrown.<br/>
        /// if this is "False" then any exceptions encountered will be suppressed.
        /// </param>
        /// 
        /// <param name="returnValue">
        /// The value to return if throwException = false.
        /// </param>
        /// 
        /// <returns>
        /// Throws the exception inside the parameter 'ex' if the parameter 'throwException' is true.<br/>
        /// Returns the parameter 'returnValue' if the parameter 'throwException' is false.
        /// </returns>
        private object? HandleException(string errorMessage, ExceptionDispatchInfo ex, bool throwException, object? returnValue)
        {
            _logger.LogError(errorMessage);
            _logger.LogInformation("Exception: {ex.SourceException}", ex.SourceException);
            // _logger.LogInformation("Stack Trace: {ex.SourceException.StackTrace}");
            // since the exception is logged, the stack trace should have already been logged
            if (throwException)
            {
                ex.Throw();
            }
            // else
            return returnValue;
        }
    }
}
