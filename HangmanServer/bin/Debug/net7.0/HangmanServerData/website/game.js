// Hangman game logic

const words = ['HELLO', 'WORLD', 'HANGMAN', 'COMPUTER', 'JAVASCRIPT']; // Add more words here
let chosenWord = words[Math.floor(Math.random() * words.length)];
let guessedWord = Array(chosenWord.length).fill('_');
let guessesLeft = 6;
let guessedLetters = [];

main();

function displayWord() 
{
    document.getElementById('wordDisplay').innerText = guessedWord.join(' ');
}

function displayGuessedLetters() 
{
    document.getElementById('guessedLetters').innerText = 'Guessed Letters: ' + guessedLetters.join(', ');
}

function guessLetter() 
{
    let input = document.getElementById('guessInput').value.toUpperCase();
    document.getElementById('guessInput').value = ''; // Clear input field after guess

    for(let j = 0; j < input.length; j++)
    {
        const c = input[j];
        if (guessedLetters.includes(c)) 
        {
            alert('Please enter a single letter or a letter you have not guessed yet.');
            return;
        }
    
        guessedLetters.push(c);
    
        if (chosenWord.includes(c)) 
        {
            for (let i = 0; i < chosenWord.length; i++) 
            {
                if (chosenWord[i] === c)
                {
                    guessedWord[i] = c;
                }
            }
        }
        else 
        {
            guessesLeft--;
        }
    }

    displayWord();
    displayGuessedLetters();

    if (guessesLeft === 0) 
    {
        alert('You lost! The word was: ' + chosenWord);
        resetGame();
    } 
    else if (!guessedWord.includes('_')) 
    {
        alert('Congratulations! You guessed the word: ' + chosenWord);
        resetGame();
    }

    document.getElementById('guessesLeft').innerText = guessesLeft;
}

function resetGame() 
{
    chosenWord = words[Math.floor(Math.random() * words.length)];
    guessedWord = Array(chosenWord.length).fill('_');
    guessesLeft = 6;
    guessedLetters = [];

    displayWord();
    displayGuessedLetters();
    document.getElementById('guessesLeft').innerText = guessesLeft;
}

function main()
{
    window.addEventListener('keypress', function (e) 
    {
        if (e.key === 'Enter') 
        {
            guessLetter();
        }
    });

    displayWord();
}