using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using MongoDB.Driver;

namespace GameOrganizer
{
    /// <summary>
    /// A WindowsForm that allows users to display or edit information about a video game collection.
    /// Utilizes MongoDB collections to read and store the game collection information.
    /// </summary>
    public partial class Collection : Form
    {
        /// <summary>
        /// Shorthand to return AppDomain.CurrentDomain.BaseDirectory string.
        /// </summary>
        public static string BaseDirectory
        {
            get { return AppDomain.CurrentDomain.BaseDirectory; }
        }

        /// <summary>
        /// Master list of games in the collection.
        /// </summary>
        private List<GameControl> games;

        /// <summary>
        /// Currently selected game control.
        /// </summary>
        private GameControl selected;

        /// <summary>
        /// A WindowsForm that displays various stats relating to the game collection.
        /// </summary>
        private CollectionStats statForm;

        /// <summary>
        /// Constructor for a collection form.
        /// </summary>
        public Collection()
        {
            InitializeComponent();

            games = new List<GameControl>();

            // Disables scroll wheel input on the sort combo box
            c_sort.MouseWheel += SortCombo_MouseWheelMove;

            // Add function to be called when the user changes how the games are sorted.
            c_sort.SelectedIndex = 0;
            c_sort.SelectedIndexChanged += SortCombo_IndexChange;

            // Disable the edit and delete buttons - no game selected.
            c_edit.Enabled = c_delete.Enabled = false;   
        }
        
        /// <summary>
        /// Adds a game to the game collection.
        /// </summary>
        /// <param name="toAdd">Game to add</param>
        private void AddGame(Game toAdd)
        {
            if (toAdd == null)
            {
                Console.WriteLine("AddGame was passed a null value");
                return;
            }

            // Games need a unique MongoDB ObjectId
            if (games.Any(control => control.Game.Id == toAdd.Id))
            {
                MessageBox.Show(String.Format(
                    "'{0}' for {1} cannot be added. A game with the same ObjectId already exists.", 
                    toAdd.Title, 
                    toAdd.Platform));
                return;
            }

            // Create a game control to display the game information
            GameControl newControl = new GameControl(toAdd);            
            games.Add(newControl);

            // Add the game to the appropriate container
            containerManager.SortGame(newControl);

            // Adds the game information to the collection stats
            statForm.GameAdded(toAdd);

            // Subscribe to Click event
            newControl.Click += Container_GameControlClick;
        }

        /// <summary>
        /// Deletes the game from the game collection.
        /// </summary>
        /// <param name="toDeletee">Game to delete</param>
        private void DeleteGame(GameControl toDelete)
        {
            if (toDelete == null)
            {
                Console.WriteLine("DeleteGame was passed a null value");
                return;
            }

            // Reset selected value if the game being removed is the selected control.
            if (selected == toDelete)
            {
                ResetSelected();
            }

            // Remove the game from its display container
            containerManager.RemoveGame(toDelete);
            
            // Remove game from the collection
            games.Remove(toDelete);
            statForm.GameRemoved(toDelete.Game);

            // Remove delegate from click event
            toDelete.Click -= Container_GameControlClick;

            toDelete.Dispose();
        }

        /// <summary>
        /// Initial loading function that queries the database for the game collection.
        /// Creates GameControl objects that contain the game information.
        /// </summary>
        private void LoadGames()
        {
            // Remove any game before loading
            while(games.Count > 0)
            {
                DeleteGame(games[0]);
            }

            // Add games to the collection
            List<Game> gameCollection = MongoDbManager.FindAll();
            foreach (var game in gameCollection)
            {
                AddGame(game);
            }
        }

        /// <summary>
        /// Update the game control with updated information from the database
        /// </summary>
        /// <param name="control">GameControl to update</param>
        private void UpdateGame(GameControl toUpdate)
        {
            if(toUpdate == null)
            {
                Console.WriteLine("UpdateGame passed a null value");
                return;
            }

            // Find updated game information from the database
            Game updated = MongoDbManager.FindOne(toUpdate.Game.Id);

            if (updated != null)
            {
                // Delete and then re-add the game control in case sorting information has changed
                DeleteGame(toUpdate);
                AddGame(updated);

                // Search game controls for updated game and re-select it
                for(int i = 0; i < games.Count; i++)
                {
                    if(games[i].Game.Id == updated.Id)
                    {
                        SetSelected(games[i]);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Unselects the current selected game and resets display information.
        /// </summary>
        private void ResetSelected()
        {
            // First reset background color before unselecting
            SetSelectedColor(GameControl.DefaultColor);

            selected = null;
            gameDisplay1.ResetDisplay();
            c_edit.Enabled = c_delete.Enabled = false;
        }

        /// <summary>
        /// Sets the selected game control and displays the game information.
        /// </summary>
        private void SetSelected(GameControl gC)
        {
            if (gC == null || gC.Game == null)
            {
                return;
            }

            // If there is a selected game control, change background color to default
            SetSelectedColor(GameControl.DefaultColor);

            // Set selected to the new game control and highlight
            selected = gC;
            SetSelectedColor(GameControl.HighlightColor);

            // Display selected game information
            gameDisplay1.Display(gC.Game);
            c_edit.Enabled = c_delete.Enabled = true;
        }

        /// <summary>
        /// Sets the background color of the selected game control.
        /// </summary>
        /// <param name="color"></param>
        private void SetSelectedColor(Color color)
        {
            // Set background color back to default
            if (selected != null)
            {
                selected.BackColor = color;
            }
        }

        /// <summary>
        /// Delegate called when clicking on the add button.
        /// Displays a form where the user enters the new object information.
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event args</param>
        private void AddButton_Click(object sender, EventArgs e)
        {
            // Display game form and add game if Ok Button selected
            using (GameForm g = new GameForm())
            {
                if (g.ShowDialog() == DialogResult.OK)
                {
                    MongoDbManager.AddGame(g.Game);
                    AddGame(g.Game);                    
                }
            }
        }

        /// <summary>
        /// Event called when the form is loaded. Loads all the games from
        /// the MongoDB "Games" collection.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Collection_Load(object sender, EventArgs e)
        {
            if (!MongoDbManager.ConnectionEstablished())
            {
                MessageBox.Show("Could not connect to the database. The program will close.");
                Close();
            }

            statForm = new CollectionStats();
            LoadGames();
        }

        /// <summary>
        /// Delegate called when a game control is selected.
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Game Control args</param>
        private void Container_GameControlClick(object sender, EventArgs e)
        {
            GameControl gC = sender as GameControl;
            if (gC == null)
            {
                return;
            }

            // If the control is already the selected, deselect it
            if (gC == selected)
            {
                ResetSelected();
            }
            else
            {
                SetSelected(gC);
            }
        }

        /// <summary>
        /// Delegate called when the delete button is clicked.
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event args</param>
        private void DeleteButton_Click(object sender, EventArgs e)
        {
            if (selected == null)
            {
                Console.WriteLine("No game currently selected. Delete failed.");
                return;
            }

            DialogResult result = MessageBox.Show("Are you sure you want to delete this game?", 
                "Confirm Delete",
                MessageBoxButtons.YesNo);
            if (result == DialogResult.Yes)
            {
                // Remove from database first while selected control still active
                MongoDbManager.RemoveGame(selected.Game);
                DeleteGame(selected);
            }
        }

        /// <summary>
        /// Delegate called when clicking on the edit button.
        /// Displays a form where the user can change the object information.
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event args</param>
        private void EditButton_Click(object sender, EventArgs e)
        {
            if (selected == null)
            {
                return;
            }

            // Create game form with the selected game as a template
            using (GameForm form = new GameForm(selected.Game))
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    // Update the selected game control if database update successful
                    if (MongoDbManager.UpdateGame(selected.Game.Id, form.Game))
                    {
                        UpdateGame(selected);
                    }
                }
            }
        }

        /// <summary>
        /// Delegate called when the game sorting logic is changed.
        /// Organizes the game collection based on the new sorting logic.
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event args</param>
        private void SortCombo_IndexChange(object sender, EventArgs e)
        {
            // Reset selected game information
            ResetSelected();

            // Updates the game sort function based on the selected sort criteria
            switch (c_sort.Text)
            {
                case "Platform":
                    containerManager.SetSortFunction(ContainerManager.SortValue.Platform);
                    break;
                case "Genre":
                    containerManager.SetSortFunction(ContainerManager.SortValue.Genre);
                    break;
                case "Release Date":
                    containerManager.SetSortFunction(ContainerManager.SortValue.Released);
                    break;
                case "Purchase Date":
                    containerManager.SetSortFunction(ContainerManager.SortValue.Purchased);
                    break;
                case "Cost":
                    containerManager.SetSortFunction(ContainerManager.SortValue.Cost);
                    break;
                case "Rating":
                    containerManager.SetSortFunction(ContainerManager.SortValue.Rating);
                    break;
                case "Title":
                default:
                    containerManager.SetSortFunction(ContainerManager.SortValue.Title);
                    break;
            }
        }

        /// <summary>
        /// Delegate called when the sort combo box recieves mouse scrollwheel input.
        /// Disables any mouse scrollwheel input.
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event args</param>
        private void SortCombo_MouseWheelMove(object sender, EventArgs e)
        {
            if (e is HandledMouseEventArgs ee)
            {
                ee.Handled = true;
            }
        }

        /// <summary>
        /// Displays the collection stats form.
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event args</param>
        private void ViewCollectionStatsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            statForm.ShowDialog();
        }

        /// <summary>
        /// Creates a form to display all of the games in the collection.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ViewGamesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (Form gameListForm = new Form())
            using(ListView list = new ListView())
            {
                gameListForm.FormBorderStyle = FormBorderStyle.FixedToolWindow;
                gameListForm.StartPosition = FormStartPosition.CenterScreen;
                gameListForm.Size = new Size(400, 300);

                // Add list view to display games
                gameListForm.Controls.Add(list);

                // Set list view properties
                list.Dock = DockStyle.Fill;
                list.View = View.Details;
                list.FullRowSelect = true;
                list.Columns.Add("Title", (int)(list.Size.Width * (3 / 4.0)));
                list.Columns.Add("Platform", list.Width - list.Columns[0].Width);
                list.Sorting = SortOrder.Ascending;

                // Add games to the list view
                for (int i = 0; i < games.Count; i++)
                {

                    ListViewItem item = new ListViewItem(new string[] { games[i].Game.Title, games[i].Game.Platform });
                    list.Items.Add(item);
                }

                // Display the form
                gameListForm.ShowDialog();
            }
        }

        /// <summary>
        /// Delegate called when the View Custom Query tool strip menu item is clicked.
        /// Creates a new Custom Query Form and displays it.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ViewCustomQueryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (CustomQueryForm customQueryForm = new CustomQueryForm())
            {
                customQueryForm.ShowDialog();
            }
        }
    }    
}
