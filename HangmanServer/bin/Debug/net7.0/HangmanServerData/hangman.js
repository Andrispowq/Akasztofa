// Hangman game logic

const words = ['HELLO', 'WORLD', 'HANGMAN', 'COMPUTER', 'JAVASCRIPT']; // Add more words here
let chosenWord = words[Math.floor(Math.random() * words.length)];
let guessedWord = Array(chosenWord.length).fill('_');
let guessesLeft = 6;
let guessedLetters = [];

function displayWord() {
    document.getElementById('wordDisplay').innerText = guessedWord.join(' ');
}

function displayGuessedLetters() {
    document.getElementById('guessedLetters').innerText = 'Guessed Letters: ' + guessedLetters.join(', ');
}

function guessLetter() {
    let input = document.getElementById('guessInput').value.toUpperCase();
    document.getElementById('guessInput').value = ''; // Clear input field after guess

    if (input.length !== 1 || guessedLetters.includes(input)) {
        alert('Please enter a single letter or a letter you have not guessed yet.');
        return;
    }

    guessedLetters.push(input);

    if (chosenWord.includes(input)) {
        for (let i = 0; i < chosenWord.length; i++) {
            if (chosenWord[i] === input) {
                guessedWord[i] = input;
            }
        }
    } else {
        guessesLeft--;
    }

    displayWord();
    displayGuessedLetters();

    if (guessesLeft === 0) {
        alert('You lost! The word was: ' + chosenWord);
        resetGame();
    } else if (!guessedWord.includes('_')) {
        alert('Congratulations! You guessed the word: ' + chosenWord);
        resetGame();
    }

    document.getElementById('guessesLeft').innerText = guessesLeft;
}

function resetGame() {
    chosenWord = words[Math.floor(Math.random() * words.length)];
    guessedWord = Array(chosenWord.length).fill('_');
    guessesLeft = 6;
    guessedLetters = [];

    displayWord();
    displayGuessedLetters();
    document.getElementById('guessesLeft').innerText = guessesLeft;
}
displayWord();
