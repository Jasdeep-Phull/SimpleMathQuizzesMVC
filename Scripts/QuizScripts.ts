/*
Notes:
Currently the TypeScript scripts in this project use jquery 3.5.29, while form validation uses jquery v3.6.0
This *should?* not be a problem, because when the Typescript code is compiled to Javascript then it *should?* use the same jquery version that the rest o the project uses
The jQuery npm package's only real purpose *should?* be to ensure that the Typescript compiler can recognise jQuery and compile
 */

/**
 * Collects the questions and answers on the page, and then makes an AJAX request to the submitUrl to create a quiz.
 * Redirects to successUrl if the AJAX request was successful, displays error messages if unsuccessful.
 * @param submitUrl The url/endpoint to submit the data to
 * @param successUrl The url to redirect to if the request is successful
 * @param numberOfQuestions The number of questions and answers to look for on the page
 */
function createQuiz(submitUrl: string, successUrl: string, numberOfQuestions: number) {
    console.log("submit called");
    if (confirm("Are you sure you want to submit your answers?")) {
        console.log("calling createQuiz");

        // collect the questions on the page, using the id of the question labels
        var viewQuestions: string[] = [];
        viewQuestions = getQuestions(numberOfQuestions);

        // collect the answers on the page, using the id of the answer inputs
        var viewAnswers: number[] = [];
        viewAnswers = getAnswers(numberOfQuestions);

        console.log(`Questions: ${viewQuestions}`);
        console.log(`Answers: ${viewAnswers}`);

        // data object
        var createData = { "Questions": viewQuestions, "UserAnswers": viewAnswers }; 
        console.log(`createData.Questions: ${createData.Questions}`);
        console.log(`createData.UserAnswers: ${createData.UserAnswers}`);

        // request body
        var sendData: string = JSON.stringify(createData);
        console.log(`sendData: ${sendData}`);

        // get the CSRF token on the page
        var verificationToken: string = $('input[name="__RequestVerificationToken"]').val().toString();
        console.log(`v token: ${verificationToken}`);

        // ajax call
        $.ajax({
            type: "POST",
            url: submitUrl,
            contentType: "application/json",
            dataType: "json",
            headers: { "RequestVerificationToken": verificationToken },
            data: sendData,
            success: function (response) {
                console.log("JSON create request was successful");
                if (response === null) {
                    console.log("json response was null");
                    alert("Successfully created new quiz");
                } else {
                    console.log(`response message: ${response.message}`);
                    alert(response.message);
                }
                // navigate to successUrl if successful
                window.location.replace(successUrl);
            },
            error: function (jqXHR, status, error) {
                console.log("JSON create request was unsuccessful");
                if (jqXHR === null) {
                    console.log("unable to get response from server");
                    alert("Request to create new quiz was unsuccessul.\nUnable to get error messages from server");
                } else {
                    if (jqXHR.responseJSON === null) {
                        console.log("unable to get JSON from response");
                        alert("Request to create new quiz was unsuccessul.\nUnable to get error messages from server");
                    }
                    else {
                        console.log(`Error ${jqXHR.status}`); // <----
                        console.log(`Error message: ${jqXHR.responseJSON.message}`);
                        alert(`${jqXHR.responseJSON.message}\nHTTP: ${jqXHR.status}`);
                    }
                }
            }
        });
    }
}

/**
 * Collects the edited answers on the page, and then makes an AJAX request to the submitUrl to update a quiz.
 * Redirects to successUrl if the AJAX request was successful, displays error messages if unsuccessful.
 * @param submitUrl The url/endpoint to submit the data to
 * @param successUrl The url to redirect to if the request is successful
 * @param numberOfQuestions The number of answers to look for on the page
 * @param quizId the id of the quiz to update
 */
function updateQuiz(submitUrl: string, successUrl: string, numberOfQuestions: number, quizId: number) {
    console.log("submit called");
    if (confirm("Are you sure you want to submit your answers?")) {
        console.log("calling updateQuiz");

        var viewAnswers: number[] = [];
        // collect the answers on the page, using the id of the answer inputs
        viewAnswers = getAnswers(numberOfQuestions);
        console.log(`Answers: ${viewAnswers}`);

        // data object
        var updateData = { "Id": quizId, "UserAnswers": viewAnswers }; // ": JSON" doesnt work 
        console.log(`updateData.Id: ${updateData.Id}`);
        console.log(`updateData.UserAnswers: ${updateData.UserAnswers}`);

        // request body
        var sendData: string = JSON.stringify(updateData);
        console.log(`sendData: ${sendData}`);

        // get the CSRF token on the page
        var verificationToken: string = $('input[name="__RequestVerificationToken"]').val().toString();
        console.log(`v token: ${verificationToken}`);

        // ajax call
        $.ajax({
            type: "POST",
            url: submitUrl,
            contentType: "application/json",
            dataType: "json",
            headers: { "RequestVerificationToken": verificationToken },
            data: sendData,
            success: function (response) {
                console.log("JSON update request was successful");
                if (response === null) {
                    console.log("json response was null");
                    alert(`Successfully edited quiz (ID: ${quizId})`);
                } else {
                    console.log(`response message: ${response.message}`);
                    alert(response.message);
                }
                // navigate to successUrl if successful
                window.location.replace(successUrl);
            },
            error: function (jqXHR, status, error) {
                console.log("JSON update request was unsuccessful");
                if (jqXHR === null) {
                    console.log("unable to get response from server");
                    alert("Request to update quiz was unsuccessul.\nUnable to get error messages from server");
                } else {
                    if (jqXHR.responseJSON === null) {
                        console.log("unable to get JSON from respons");
                        alert("Request to update quiz was unsuccessul.\nUnable to get error messages from server");
                    }
                    else {
                        console.log(`Error ${jqXHR.status}`); // <----
                        console.log(`Error message: ${jqXHR.responseJSON.message}`);
                        alert(`${jqXHR.responseJSON.message}\nHTTP ${jqXHR.status}`);
                    }
                }
            }
        });
    }
}

/**
 * Finds the questions on the page, by the id of the HTML element they are contained in.
 * Returns the questions as a list of strings
 * @param numberOfQuestions The number of questions to look for on the page
 * @returns the questions on the page, as a list of strings
 */
function getQuestions(numberOfQuestions: number): string[] {
    console.log("getQuestions called");
    var questions: string[] = [];
    console.log("loop begin");
    for (var i: number = 0; i < numberOfQuestions; i++) {
        var questionId: string = `#question${i.toString()}`;
        console.log(`qId: ${questionId}`);

        var question: string = $(questionId).text();
        console.log(`  question: ${question}`);

        questions.push(question);
    }
    console.log(`questions: ${questions}`);
    return questions;
}

/**
 * Finds the answers on the page, by the id of the HTML input they are contained in.
 * Returns the answers as a list of numbers (number[]).
 * @param numberOfQuestions The number of answers to look for on the page
 * @returns the answers on the page, as a list of numbers (number[])
 */
function getAnswers(numberOfQuestions: number): number[] {
    console.log("getAnswers called");
    var userAnswers: number[] = [];
    for (var i = 0; i < numberOfQuestions; i++) {
        var answerId: string = `#answer${i.toString()}`;
        console.log(`aId: ${answerId}`);

        var stringUserAnswer: string = $(answerId).val().toString();
        console.log(`  stringAnswer: ${stringUserAnswer}`);

        // check if the answer is valid, return null if the answer is invalid
        var userAnswer: number = checkAnswer(stringUserAnswer);
        console.log(`    userAnswer: ${userAnswer}`);

        userAnswers.push(userAnswer);
    }
    console.log(`userAnswers: ${userAnswers}`);
    return userAnswers;
}

/**
 * Checks if an answer is valid. Checks if the answer is an integer.
 * Returns the answer as an integer if it is valid.
 * (client-side validation also checks the answers and blocks submitting when any answer is not an integer, or if it is above the maximum value or below the minimum value)
 * @param userAnswer the answer, as a string, to check against the regex
 * @returns the answer as an integer (it has the number data type, but it is the result of the parseInt() method)
 */
function checkAnswer(userAnswer: string): number {
    console.log("checkAnwer called");
    var tempAnswer: string = userAnswer.trim();
    console.log(`3answer: ${userAnswer}, tempAnswer: ${tempAnswer}`);
    // check against regex
    if (/^(-)?(0|([1-9](\d)*))$/.test(tempAnswer)) {
        var intAnswer: number = parseInt(tempAnswer, 10);
        console.log(`3tempAnswer: ${tempAnswer}, intAnswer: ${intAnswer}`);
        // check if parsed answer is NaN
        if (isNaN(intAnswer) === true) {
            // log error
            console.log(`3answer: ${userAnswer} is invalid, was Nan after parseInt()`);
            return null;
        }
        else {
            console.log(`3answer: ${userAnswer} is valid`);
            return intAnswer;
        }
    }
    else {
        console.log(`3answer: ${userAnswer}, " is invalid`);
        return null;
    }
}