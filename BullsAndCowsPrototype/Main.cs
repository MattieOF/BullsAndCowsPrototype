using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;

namespace BullsAndCowsPrototype
{
    public partial class Main : Form
    {
        private enum GameMode
        {
            Number,
            Word
        }

        private string prevText = "";
        private GameMode gameMode = GameMode.Number;

        private List<string> validWords = new List<string>();

        private int numberLength = 4;
        private string computerNumber, computerWord;

        public Main()
        {
            InitializeComponent();
            LoadWords();
            SetMode(GameMode.Number);
        }

        private void LoadWords(string filename = "words.txt")
        {
            TextReader reader = File.OpenText(filename);
            var words = reader.ReadToEnd().Split('\n');
            reader.Close();

            validWords.Clear();
            foreach (string word in words)
                validWords.Add(string.Concat(word.Where(c => !char.IsWhiteSpace(c))));

            // TDOO: Could probably be done better with a validWords.RemoveAll() 

            List<string> wordsForRemoval = new List<string>();
            List<char> seenChars = new List<char>();
            for (int i = 0; i < validWords.Count; i++)
            {
                foreach (char c in validWords[i])
                {
                    if (seenChars.Contains(c))
                    {
                        wordsForRemoval.Add(validWords[i]);
                        continue;
                    }
                    seenChars.Add(c);
                }
                seenChars.Clear();
            }

            foreach (string str in wordsForRemoval)
                validWords.Remove(str);
        }

        private void Main_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
                Close();
        }

        private void SetMode(GameMode mode)
        {
            modeSelector.SelectedIndex = (int)mode;
            gameMode = mode;
            guessInput.Text = "";
            resultsBox.Items.Clear();

            switch (gameMode)
            {
                case GameMode.Number:
                    SetNumber();
                    break;
                case GameMode.Word:
                    Random random = new Random();
                    computerWord = validWords[random.Next(0, validWords.Count)];
                    break;
            }
        }

        private void SetNumber()
        {
            Debug.Assert(numberLength <= 10, "Number length must be no more than 10");

            List<char> digits = new List<char>{ '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
            StringBuilder sb = new StringBuilder();
            Random random = new Random();

            for (int i = 0; i < numberLength; i++)
            {
                int digitIndex = random.Next(0, digits.Count);
                sb.Append(digits[digitIndex]);
                digits.RemoveAt(digitIndex);
            }

            computerNumber = sb.ToString();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            About about = new About();
            about.Show();
        }

        private void guessInput_TextChanged(object sender, EventArgs e)
        {
            // Check for wrong length
            if (guessInput.Text.Length > 4)
                guessInput.Text = prevText;

            // If in number mode, check its a number
            if (gameMode == GameMode.Number && !guessInput.Text.All(char.IsDigit))
                guessInput.Text = prevText;

            // If in word mode, check its using only letters
            if (gameMode == GameMode.Word && !guessInput.Text.All(char.IsLetter))
                guessInput.Text = prevText;

            // Check for duplicate characters
            List<char> seenChars = new List<char>();
            foreach (char c in guessInput.Text)
            {
                if (seenChars.Contains(c))
                {
                    guessInput.Text = prevText;
                    prevText = guessInput.Text;
                    return;
                }
                seenChars.Add(c);
            }
            
            prevText = guessInput.Text;
        }

        private void modeSelector_SelectedIndexChanged(object sender, EventArgs e)
        {
            SetMode((GameMode)modeSelector.SelectedIndex);
        }

        private void newGameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetMode(gameMode);
        }

        private void regenerateValueToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetMode(gameMode);
        }

        private void regenerateAndViewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            regenerateValueToolStripMenuItem_Click(sender, e);
            viewComputersValueToolStripMenuItem_Click(sender, e);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            CheckAnswer();
        }

        private void CheckAnswer()
        {
            string currentGuess = guessInput.Text;
            string computerValue = gameMode == GameMode.Number ? computerNumber : computerWord;
            int bulls = 0, cows = 0;

            if (currentGuess.Length != computerValue.Length)
            {
                MessageBox.Show($"Guess must be {computerValue.Length} characters long!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            for (int i = 0; i < computerValue.Length; i++)
            {
                if (currentGuess[i] == computerValue[i])
                {
                    bulls++;
                }
            }

            for (int i = 0; i < computerValue.Length; i++)
            {
                if (computerValue.Contains(currentGuess[i]))
                    cows++;
            }

            AddResult(bulls, cows);

            // If bulls = numberLength, game won!
            if (bulls == numberLength)
                GameWon(computerValue);

            guessInput.Text = "";
            resultsBox.TopIndex = resultsBox.Items.Count - 1;
        }

        private void GameWon(string value)
        {
            DialogResult result = MessageBox.Show($"Game won after {resultsBox.Items.Count} guesses!!" +
                $"\nExport guesses to file?", "Game Won", MessageBoxButtons.YesNo, MessageBoxIcon.Information);


            if (result == DialogResult.Yes)
            {
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Title = "Export Results";
                sfd.Filter = "Text Files|*.txt|All Files|*.*";
                sfd.DefaultExt = "txt";
                sfd.CheckPathExists = true;
                DialogResult sfdResult = sfd.ShowDialog();

                if (sfdResult == DialogResult.OK)
                {
                    TextWriter writer = File.CreateText(sfd.FileName);
                    writer.WriteLine($"Guessed {value} in {resultsBox.Items.Count} guesses");
                    foreach (string guess in resultsBox.Items)
                        writer.WriteLine(guess);
                    writer.Close();
                }
            }

            SetMode(gameMode);
        }

        private void AddResult(int bulls, int cows)
        {
            resultsBox.Items.Add($"{guessInput.Text}: {bulls} bulls, {cows} cows");
        }

        private void setValueToInputToolStripMenuItem_Click(object sender, EventArgs e)
        {
            computerNumber = guessInput.Text;
            guessInput.Text = "";
        }

        private void guessInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                CheckAnswer();
        }

        private void howToPlayToolStripMenuItem_Click(object sender, EventArgs e)
        {
            HowToPlay window = new HowToPlay();
            window.Show();
        }

        private void viewComputersValueToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show($"The computers current {(gameMode == GameMode.Number ? "number" : "word")} is {(gameMode == GameMode.Number ? computerNumber : computerWord)}", "Current Value", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
