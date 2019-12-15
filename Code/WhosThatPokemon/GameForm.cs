using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;

namespace WhosThatPokemon
{
    /// <summary>
    /// Form that holds the game logic.
    /// </summary>
    public partial class GameForm : Form
    {
        /// <summary>
        /// Determines if game logic code is run.
        /// </summary>
        bool playing = false;

        /// <summary>
        /// Number of attempts remaining for the round.
        /// </summary>
        private int attemptsRemaining;

        /// <summary>
        /// Number of rounds remaining for the game.
        /// </summary>
        private int roundsRemaining;

        /// <summary>
        /// Master list of Pokemon.
        /// </summary>
        private List<Pokemon> pokemon;

        /// <summary>
        /// List of Pokemon available to choose based on game settings.
        /// </summary>
        private List<Pokemon> toChoose;

        /// <summary>
        /// Chosen Pokemon for the player to guess.
        /// </summary>
        private Pokemon chosen;

        /// <summary>
        /// Random number generator.
        /// </summary>
        private Random randomGenerator;        

        /// <summary>
        /// Game settingsused to determine Pokemon to choose,
        /// the game restrictions, and the hints given.
        /// </summary>
        private Settings gameSettings;

        /// <summary>
        /// Time remaining for the round.
        /// </summary>
        private TimeSpan timeRemaining;

        /// <summary>
        /// Gets the game time in a string format.
        /// </summary>
        private string GameTime 
        { 
            get 
            { 
                return timeRemaining.Minutes + ":" + 
                    timeRemaining.Seconds.ToString("00"); 
            } 
        }

        /// <summary>
        /// Create an instance of GameForm.
        /// </summary>
        public GameForm()
        {
            InitializeComponent();

            randomGenerator = new Random();
            LoadPokemon();

            // During game textbox should always have focus
            textBox1.LostFocus += textBox1_LostFocus;
        }

        /// <summary>
        /// Adds a guess to the guess list view.
        /// </summary>
        /// <param name="name">Player guess</param>
        private void AddGuess(string name)
        {
            // Display guess information
            string msg = "";
            double strength = GetGuessStrength(name, ref msg);
            if (msg == "")
            {
                msg = "No similarities in: " + gameSettings.hints;
            }
            c_hints.Text = msg;
            c_pokemon.Text = name;

            // If Pokemon already guessed, do not add to guess list.
            if (listView1.FindItemWithText(name) != null)
            {
                return;
            }

            // Text color determined by guess strength.
            ListViewItem item = new ListViewItem(name, (int)strength);
            if (strength < 1) { item.ForeColor = Color.Blue; }
            else if (strength < 4) { item.ForeColor = Color.Aquamarine; }
            else if (strength < 6) { item.ForeColor = Color.LightCoral; }
            else { item.ForeColor = Color.Red; }

            // Insert guess into list based on guess strength.
            // Use image index to store guess strength.
            bool added = false;
            for (int i = 0; i < listView1.Items.Count; i++)
            {
                if (listView1.Items[i].ImageIndex <= item.ImageIndex)
                {
                    listView1.Items.Insert(i, item);
                    added = true;
                    break;
                }
            }
            if (!added)
            {
                listView1.Items.Add(item);
            }

            // Resize list view column width if neccessary
            DisplayFullColumn(listView1);

            // If game based on guess count, reduce guess count.
            if (gameSettings.hasLimit)
            {
                attemptsRemaining--;
                c_guess.Text = attemptsRemaining.ToString();
                if (attemptsRemaining <= 0)
                {
                    EndRound(false);
                }
            }
        }

        /// <summary>
        /// Determies if two Pokemon belong to same family.
        /// </summary>
        /// <param name="x">First Pokemon</param>
        /// <param name="y">Second Pokemon</param>
        /// <returns>True if same family</returns>
        private bool AreFamily(Pokemon x, Pokemon y)
        {
            // Traverse all previous family members.
            Pokemon temp = x;
            while (temp.PrevEvo != "")
            {
                temp = toChoose.Find(a => a.Name == temp.PrevEvo);

                if (temp == null) 
                { 
                    break; 
                }
                if (temp.Name == y.Name)
                {
                    return true;
                }
            }

            // Traverse all next family members.
            temp = x;
            while (temp.NextEvo != "")
            {
                temp = toChoose.Find(a => a.Name == temp.NextEvo);
                if (temp == null) 
                { 
                    break; 
                }
                if (temp.Name == y.Name)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Sets the chosen pokemon to a new value.
        /// </summary>
        public void ChooseNextPokemon()
        {
            Pokemon newPokemon = null;
            do
            {
                newPokemon = toChoose[randomGenerator.Next(0, toChoose.Count)];
            } while (newPokemon == chosen);

            chosen = newPokemon;
        }

        /// <summary>
        /// Resizes the guess list view column width so that
        /// the first column is fully displayed. Width depends
        /// on the vertical scroll bar being visible.
        /// </summary>
        /// <param name="displayScroll">Vertical scroll bar displayed?</param>
        private void DisplayFullColumn(ListView a)
        {
            // If items overflow, reduce column size due to scroll bar
            if (a.Items.Count != 0 &&
                a.Height - a.Items[0].Bounds.Y <
                a.Items.Count * a.Items[0].Bounds.Height)
            {
                a.Columns[0].Width = (a.Width -
                    SystemInformation.VerticalScrollBarWidth);
            }
            else
            {
                a.Columns[0].Width = a.Width;
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
            listView1.Items.Clear();

            // Reset text
            c_time.Text = "0.00";
            c_guess.Text = "0";
            c_rounds.Text = "0";
            c_header.Text = "Last Guess:";
            c_pokeImage.Hide();
            c_pokemon.ResetText();
            c_hints.ResetText();
            textBox1.ResetText();

            // Disable input
            button1.Visible = false;
            textBox1.Enabled = false;

            // Reset background colors
            c_header.BackColor = c_pokemon.BackColor = Color.White;
        }

        /// <summary>
        /// Sets the playing values to the end round values.
        /// </summary>
        /// <param name="win">Did player win?</param>
        private void EndRound(bool win)
        {
            // Reset playing variables 
            playing = false;
            timer1.Stop();

            // Reset text
            textBox1.ResetText();
            c_hints.ResetText();

            // Display chosen Pokemon
            c_header.Text = "It's";
            c_pokemon.Text = chosen.Name + "!";
            c_pokeImage.Visible = true;

            // Background color changes depending on win value.
            c_header.BackColor = c_pokemon.BackColor =
                win ? Color.LimeGreen : Color.FromArgb(255, 64, 64);

            // Allow for advance to next round
            textBox1.Enabled = false;
            button1.Visible = true;
            button1.Focus();
        }

        /// <summary>
        /// Return a value that determines the strength of the guess
        /// compared to the chosen Pokemon. Strength determined based
        /// on the number of similarities compared to the total
        /// similarities tested. 
        /// </summary>
        /// <param name="name">Player guess.</param>
        /// <param name="msg">Reference to the message to be displayed.</param>
        /// <returns>Strength of guess.</returns>
        private double GetGuessStrength(string name, ref string msg)
        {
            // Do nothing if the guess is not valid.
            Pokemon guess = toChoose.Find(a => a.Name == name);
            if (guess == null) return 0;

            // Strength values
            int maxStrength = 0;
            double strength = 0;

            // Family guess strength: +3
            if (Hints.Family == (gameSettings.hints & Hints.Family))
            {
                maxStrength++;
                if(AreFamily(chosen, guess))
                {
                    msg += "Same family\n";
                    strength += 3;
                }
            }

            // Type guess strength: +1.5
            if(Hints.Type == (gameSettings.hints & Hints.Type))
            {
                maxStrength += chosen.Types.Length;

                // Keep track of number of type similarities.
                int similarities = chosen.SameTypeCount(guess);
                if (similarities != 0)
                {
                    msg += String.Format("{0} in common\n",
                        similarities == 1 ? "1 type" : similarities + " types");
                    strength += similarities * 1.5;
                }
            }

            // Ability guess strength: +1
            if(Hints.Ability == (gameSettings.hints & Hints.Ability))
            {
                maxStrength += chosen.Abilities.Length;

                // Keep track of number of ability similarities.
                int similarities = chosen.SameAbilityCount(guess);
                if (similarities != 0)
                {
                    msg += String.Format("{0} in common\n",
                        similarities == 1 ? "1 ability" : similarities + " abilities");
                    strength += similarities;
                }
            }

            // Generation guess strength: +1
            if(Hints.Generation == (gameSettings.hints & Hints.Generation))
            {
                maxStrength++;
                if(guess.Gen == chosen.Gen)
                {
                    strength++;
                    msg += "Same generation\n";
                }
            }

            // First letter guess strength: +1
            if(Hints.FirstLetter == (gameSettings.hints & Hints.FirstLetter))
            {
                maxStrength++;
                if(guess.Name[0] == chosen.Name[0])
                {
                    strength++;
                    msg += "Starts with the same letter\n";
                }
            }

            // Name length guess strength: +1
            if(Hints.NameLength == (gameSettings.hints & Hints.NameLength))
            {
                maxStrength++;
                if(guess.Name.Length == chosen.Name.Length)
                {
                    strength++;
                    msg += "Same number of letters (" + guess.Name.Length + ")\n";
                }
            }

            // Return strength value
            return strength / maxStrength * 10;
        }

        /// <summary>
        /// Load information from XML document.
        /// </summary>
        public void LoadPokemon()
        {
            pokemon = new List<Pokemon>();
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            // Read in values from document
            XDocument doc = XDocument.Load("pokemon.xml");
            foreach (var p in doc.Descendants("Pokemon"))
            {
                Pokemon poke = new Pokemon(
                    p.Element("Name").Value,
                    Convert.ToInt16(p.Element("Gen").Value),
                    p.Element("Type1").Value,
                    p.Element("Type2").Value,
                    p.Element("Ability1").Value,
                    p.Element("Ability2").Value,
                    p.Element("Ability3").Value,
                    p.Element("PrevEvo").Value,
                    p.Element("NextEvo").Value);
                pokemon.Add(poke);
            }
            sw.Stop();
            Console.WriteLine(sw.ElapsedMilliseconds);
        }        

        /// <summary>
        /// Displays the New Game Form.
        /// </summary>
        private void NewGame() 
        {
            // Stop timer
            timer1.Stop();

            // Display new game form
            using (var newGameForm = new NewGame())
            {
                if (newGameForm.ShowDialog() == DialogResult.OK)
                {   // End current game and start a new game
                    EndGame();

                    // Set new game settings
                    gameSettings = newGameForm.gameSettings;
                    roundsRemaining = gameSettings.numRounds;

                    // Set avaiable Pokemon to be chosen based on settings

                    toChoose = pokemon.FindAll(a => gameSettings.generations.Contains(a.Gen));

                    textBox1.AutoCompleteCustomSource.Clear();
                    textBox1.AutoCompleteCustomSource.AddRange(toChoose.Select(a => a.Name).ToArray());
                    
                    NextRound();
                }
                else if (playing && gameSettings.isTimed)
                {   // Resume current game timer
                    timer1.Start();
                }
            }
        }

        /// <summary>
        /// Sets variables to start a new round.
        /// </summary>
        private void NextRound()
        {
            // Reduce rounds remaining
            button1.Visible = false;
            roundsRemaining--;
            c_rounds.Text = roundsRemaining.ToString();

            // Reset display information
            listView1.Items.Clear();
            DisplayFullColumn(listView1);
            c_header.Text = "Last Guess:";
            c_pokemon.ResetText();
            c_pokemon.BackColor = Color.White;
            c_header.BackColor = Color.White;

            // Choose next pokemon
            c_pokeImage.Visible = false;
            ChooseNextPokemon();
            c_pokeImage.Load("PokemonSprites\\" + chosen.Name + ".jpg");

            // Enable input
            textBox1.Enabled = true;
            textBox1.Focus();

            // Set round limits based on game settings
            playing = true;
            if (gameSettings.hasLimit)
            {
                attemptsRemaining = gameSettings.numGuesses;
                c_guess.Text = attemptsRemaining.ToString();
            }
            else { c_guess.Text = "--"; }
            if (gameSettings.isTimed)
            {
                timeRemaining = gameSettings.time;
                c_time.Text = GameTime;
                timer1.Start();
            }
            else { c_time.Text = "----"; }
        }

        /// <summary>
        /// Moves onto next round of game.
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event args</param>
        private void button1_Click(object sender, EventArgs e)
        {
            if (roundsRemaining > 0)
            {
                NextRound();
            }
            else
            {
                EndGame();
                MessageBox.Show("Thanks for playing!");
            }
        }

        /// <summary>
        /// Redisplay previous guess information.
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event args</param>
        private void listView1_DoubleClick(object sender, EventArgs e)
        {
            AddGuess(listView1.SelectedItems[0].Text);
        }

        /// <summary>
        /// Calls function to start new game.
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event args</param>
        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            NewGame();
        }

        /// <summary>
        /// Delegate called to enter player guess.
        /// </summary>
        /// <param name="sender">ender</param>
        /// <param name="e">Key Event args</param>
        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            // Round is over so do nothing
            if (!playing)
            {
                textBox1.ResetText();
                e.Handled = true;
                return;
            }

            if (e.KeyCode == Keys.Enter)
            {
                // Find Pokemon corresponding to guess
                Pokemon guess = toChoose.Find(
                    a => String.Compare(a.Name, textBox1.Text, true) == 0);

                // End game if guess correct
                if (guess == chosen)
                {
                    EndRound(true);
                }
                // Add guess if a corresponding Pokemon was found
                else if (guess != null)
                {
                    AddGuess(guess.Name);
                }

                textBox1.ResetText();
                e.Handled = true;
            }
        }

        /// <summary>
        /// Always have focus on text box during gameplay.
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event args</param>
        void textBox1_LostFocus(object sender, EventArgs e)
        {
            textBox1.Focus();
        }

        /// <summary>
        /// Reduces time remaining by timer interval.
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event args</param>
        private void timer1_Tick(object sender, EventArgs e)
        {
            // Reduce time remaining and display.
            timeRemaining = timeRemaining.Subtract(
                new TimeSpan(0, 0, 0, 0, timer1.Interval));
            c_time.Text = GameTime;

            /* 
             * If no time remaining, end round with player having not won.
             * Since timespan uses milliseconds, check time manually.
             * Doing so with TimeSpan.Zero causes misrepresentation of the
             * display values.
             */
            if (timeRemaining.Hours <= 0 && timeRemaining.Minutes <= 0 &&
                timeRemaining.Seconds <= 0)
            {
                EndRound(false);
            }
        }

        /// <summary>
        /// Displays the Pokedex View Form.
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event args</param>
        private void viewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var ViewForm = new Viewer(pokemon))
            {
                timer1.Stop();
                ViewForm.ShowDialog();

                if (playing && gameSettings.isTimed)
                {
                    timer1.Start();
                }
            }
        }        
    }
}
