using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace WhosThatPokemon
{
    /// <summary>
    /// Form that holds the guessing game logic.
    /// </summary>
    public partial class GameForm : Form
    {
        /// <summary>
        /// Maintains the Pokemon for the program.
        /// </summary>
        private PokemonManager pokeManager;

        /// <summary>
        /// Pokemon available to choose based on game settings.
        /// </summary>
        private Pokemon[] toChoose;

        /// <summary>
        /// Chosen Pokemon for the player to guess.
        /// </summary>
        private Pokemon chosen;

        /// <summary>
        /// Game settings used to determine Pokemon to choose,
        /// the game restrictions, and the hints given.
        /// </summary>
        private Settings gameSettings;

        /// <summary>
        /// Determines if game logic code is run.
        /// </summary>
        private bool playing = false;

        /// <summary>
        /// Create an instance of the GameForm.
        /// </summary>
        public GameForm()
        {
            InitializeComponent();
            
            pokeManager = new PokemonManager();
            toChoose = new Pokemon[0];
        }

        /// <summary>
        /// Adds a guess to the guess list view and displays the guess results.
        /// </summary>
        /// <param name="name">Player guess</param>
        private void AddGuess(Pokemon guess)
        {
            if(guess == null || guess.Name == null)
            {
                Console.WriteLine("AddGuess passed a null value");
                return;
            }

            // If Pokemon already guessed, do not add to guess list
            ListViewItem item = guessList.FindItemWithText(guess.Name);
            if (item != null)
            {
                // Display previous guess information
                hintsText.Text = item.ToolTipText;
                pokemonText.Text = guess.Name;
                return;
            }

            // Get the guess strength information
            string msg = "";
            int strength = GetGuessStrength(guess, ref msg);

            // Insert guess into list based on guess strength.
            guessList.AddGuess(guess.Name, strength, msg);

            // Update display
            ReduceAttemptsRemaining();
            if (gameSettings.OutOfGuesses)
            {
                EndRound(false);
            }  
            else
            {
                hintsText.Text = msg;
                pokemonText.Text = guess.Name;
            }
        }

        /// <summary>
        /// Sets all values to end game values.
        /// </summary>
        private void EndGame()
        {
            // Reset playing variables
            playing = false;
            chosen = null;
            roundTimer.Stop();

            // Reset display
            timeRemainingText.Text = "0.00";
            guessesRemainingText.Text = "0";
            roundRemainingText.Text = "0";
            headerText.Text = "Last Guess:";
            pokemonText.ResetText();
            hintsText.ResetText();
            guessTextBox.ResetText();
            guessList.Items.Clear();
            pokemonImage.Hide();

            // Disable input
            nextButton.Visible = false;
            guessTextBox.Enabled = false;
            guessList.DoubleClick -= GuessList_DoubleClick;

            // Reset background colors
            headerText.BackColor = pokemonText.BackColor = Color.White;
        }

        /// <summary>
        /// Ends the round and displays the selected Pokemon.
        /// </summary>
        /// <param name="win">Did player win?</param>
        private void EndRound(bool win)
        {
            // Reset playing variables 
            playing = false;
            roundTimer.Stop();

            // Reset text
            guessTextBox.ResetText();
            hintsText.ResetText();
            guessTextBox.Enabled = false;

            // Display chosen Pokemon
            headerText.Text = "It's";
            pokemonText.Text = chosen.Name + "!";
            pokemonImage.Visible = true;

            // Background color changes depending on if the player won
            headerText.BackColor = pokemonText.BackColor =
                win ? Color.LimeGreen : Color.FromArgb(255, 64, 64);

            // Disable guessList double click so display will not update
            guessList.DoubleClick -= GuessList_DoubleClick;

            // Allow for advance to next round
            nextButton.Visible = true;
            nextButton.Focus();
        }

        /// <summary>
        /// Return a value that determines the strength of the guess
        /// compared to the chosen Pokemon. Higher values indicate
        /// increased similarities based on the game settings.
        /// </summary>
        /// <param name="guessed">Player guess.</param>
        /// <param name="msg">Reference to the message to be displayed.</param>
        /// <returns>Strength of the guess.</returns>
        private int GetGuessStrength(Pokemon guessed, ref string msg)
        {
            if (guessed == null)
            {
                Console.WriteLine("GetGuessStrength passed a null value.");
                return 0;
            }
            if(chosen == null)
            {
                Console.WriteLine("No selected Pokemon to compare.");
                return 0;
            }

            // Set values
            msg = msg ?? "";
            int maxStrength = 0;
            double strength = 0;

            // Family guess strength: +3
            if (Hints.Family == (gameSettings.Hints & Hints.Family))
            {
                maxStrength++;
                if(pokeManager.AreFamily(chosen.Name, guessed.Name))
                {
                    msg += "Same family\n";
                    strength += 3;
                }
            }

            // Type guess strength: +1.5
            if(Hints.Type == (gameSettings.Hints & Hints.Type))
            {
                maxStrength += chosen.Types.Length;

                // Keep track of number of type similarities.
                int similarities = pokeManager.SameTypeCount(chosen, guessed);
                if (similarities != 0)
                {
                    msg += String.Format("{0} in common\n",
                        similarities == 1 ? "1 type" : similarities + " types");
                    strength += similarities * 1.5;
                }
            }

            // Ability guess strength: +1
            if(Hints.Ability == (gameSettings.Hints & Hints.Ability))
            {
                maxStrength += chosen.Abilities.Length;

                // Keep track of number of ability similarities.
                int similarities = pokeManager.SameAbilityCount(chosen, guessed);
                if (similarities != 0)
                {
                    msg += String.Format("{0} in common\n",
                        similarities == 1 ? "1 ability" : similarities + " abilities");
                    strength += similarities;
                }
            }

            // Generation guess strength: +1
            if(Hints.Generation == (gameSettings.Hints & Hints.Generation))
            {
                maxStrength++;
                if(guessed.Gen == chosen.Gen)
                {
                    strength++;
                    msg += "Same generation\n";
                }
            }

            // First letter guess strength: +1
            if(Hints.FirstLetter == (gameSettings.Hints & Hints.FirstLetter))
            {
                maxStrength++;
                if(guessed.Name[0] == chosen.Name[0])
                {
                    strength++;
                    msg += "Starts with the same letter\n";
                }
            }

            // Name length guess strength: +1
            if(Hints.NameLength == (gameSettings.Hints & Hints.NameLength))
            {
                maxStrength++;
                if(guessed.Name.Length == chosen.Name.Length)
                {
                    strength++;
                    msg += "Same number of letters (" + guessed.Name.Length + ")\n";
                }
            }

            // Set an empty message to a default message
            if (msg == "")
            {
                msg = "No similarities in: " + gameSettings.Hints;
            }

            // Return strength value
            if (maxStrength == 0)
            {
                maxStrength = 1;
            }
            return (int)(strength / maxStrength * 10);
        }    

        /// <summary>
        /// Displays the New Game Form.
        /// </summary>
        private void NewGame() 
        {
            // Stop timer
            roundTimer.Stop();

            // Display new game form
            using (var newGameForm = new NewGameForm())
            {
                if (newGameForm.ShowDialog() == DialogResult.OK)
                {
                    EndGame();

                    // Set new game settings
                    gameSettings = newGameForm.GameSettings;
                    if(gameSettings == null)
                    {
                        MessageBox.Show("Error creating game settings. Could not start game.");
                        return;
                    }

                    // Set avaiable Pokemon to be chosen based on settings
                    toChoose = pokeManager.GetSubset(a => gameSettings.Generations.Contains(a.Gen)).ToArray();
                    guessTextBox.AutoCompleteCustomSource.Clear();
                    guessTextBox.AutoCompleteCustomSource.AddRange(toChoose.Select(a => a.Name).ToArray());
                    
                    NextRound();
                }
                else if (playing && gameSettings.IsTimed)
                {   // Resume current game timer
                    roundTimer.Start();
                }
            }
        }

        /// <summary>
        /// Sets variables to start a new round.
        /// </summary>
        private void NextRound()
        {
            // End game if no more rounds remain
            if (gameSettings.RoundsRemaining <= 0)
            {
                MessageBox.Show("No more round remaining. Thanks for playing!");
                EndGame();
                return;
            }

            // Reduce rounds remaining
            ReduceRoundsRemaining();

            // Choose next pokemon
            pokemonImage.Visible = false;
            chosen = pokeManager.SelectRandomPokemon(toChoose);
            if (chosen == null)
            {
                MessageBox.Show("Could not select a Pokemon. Ending current game.");
                EndGame();
                return;
            }
            pokemonImage.ImageLocation = "PokemonSprites\\" + chosen.Name + ".jpg";

            // Reset display information
            hintsText.Text = "";
            headerText.Text = "Last Guess:";
            guessList.Items.Clear();
            guessList.UpdateColumnWidth();
            pokemonText.ResetText();
            pokemonText.BackColor = Color.White;
            headerText.BackColor = Color.White;

            // Enable input
            guessList.Enabled = true;
            guessTextBox.Enabled = true;
            guessTextBox.Focus();
            guessList.DoubleClick += GuessList_DoubleClick;

            // Set round limits based on game settings
            playing = true;
            nextButton.Visible = false;
            gameSettings.ResetRoundLimits();
            guessesRemainingText.Text = gameSettings.GuessesRemainingString;
            timeRemainingText.Text = gameSettings.TimeRemainingString;
            if (gameSettings.IsTimed)
            {
                roundTimer.Start();
            }
        }

        /// <summary>
        /// Reduces the attempts remaining and updates display
        /// </summary>
        private void ReduceAttemptsRemaining()
        {
            gameSettings.ReduceAttemptsRemaining();
            guessesRemainingText.Text = gameSettings.GuessesRemainingString;
        }

        /// <summary>
        /// Reduces the rounds remaining and updates display
        /// </summary>
        private void ReduceRoundsRemaining()
        {
            gameSettings.ReduceRoundsRemaining();
            roundRemainingText.Text = gameSettings.RoundsRemaining.ToString();
        }

        /// <summary>
        /// Reduces the time remaining and updates display
        /// </summary>
        /// <param name="elapsedMilliseconds">Elapsed milliseconds</param>
        private void ReduceTimeRemaining(int elapsedMilliseconds)
        {
            gameSettings.ReduceTimeRemaining(elapsedMilliseconds);
            timeRemainingText.Text = gameSettings.TimeRemainingString;
        }

        /// <summary>
        /// Redisplay previous guess information.
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event args</param>
        private void GuessList_DoubleClick(object sender, EventArgs e)
        {
            if(guessList.SelectedItems.Count > 0)
            {
                Pokemon guess = pokeManager.GetPokemon(guessList.SelectedItems[0].Text);
                AddGuess(guess);
            }
        }

        /// <summary>
        /// Reduces time remaining by timer interval.
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event args</param>
        private void GameTimer_Tick(object sender, EventArgs e)
        {
            // Reduce time remaining and display.
            ReduceTimeRemaining(roundTimer.Interval);

            // Round is over, player loses
            if (gameSettings.OutOfTime)
            {
                EndRound(false);
            }
        }

        /// <summary>
        /// Delegate called to enter player guess.
        /// </summary>
        /// <param name="sender">ender</param>
        /// <param name="e">Key Event args</param>
        private void GuessTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            // Round is over so do nothing
            if (!playing)
            {
                guessTextBox.ResetText();
                e.Handled = true;
                return;
            }

            if (e.KeyCode == Keys.Enter)
            {
                // Find Pokemon corresponding to guess
                Pokemon guess = pokeManager.GetPokemon(guessTextBox.Text);

                // End game if guess correct
                if (guess == chosen)
                {
                    EndRound(true);
                }
                // Add guess
                else
                {
                    AddGuess(guess);
                }

                guessTextBox.ResetText();
                e.Handled = true;
            }
        }

        /// <summary>
        /// Always have focus on text box during gameplay.
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event args</param>
        void GuessTextBox_LostFocus(object sender, EventArgs e)
        {
            guessTextBox.Focus();
        }

        /// <summary>
        /// Calls function to start new game.
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event args</param>
        private void NewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            NewGame();
        }

        /// <summary>
        /// Moves onto next round of game.
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event args</param>
        private void NextButton_Click(object sender, EventArgs e)
        {
            NextRound();
        }

        /// <summary>
        /// Displays the Pokedex View Form.
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event args</param>
        private void ViewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var pokedex = new PokedexForm(pokeManager))
            {
                roundTimer.Stop();
                pokedex.ShowDialog();

                if (playing && gameSettings.IsTimed)
                {
                    roundTimer.Start();
                }
            }
        }

        /// <summary>
        /// Delegate that loads Pokemon from the database when the form is loaded.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GameForm_Load(object sender, EventArgs e)
        {
            if(!pokeManager.LoadPokemon())
            {
                // Application failed to connect to the database
                MessageBox.Show(
                    "Could not load Pokemon from the database. Exiting the program",
                    "Error");
                Close();
            }

            // During game textbox should always have focus
            guessTextBox.LostFocus += GuessTextBox_LostFocus;
        }
    }
}
