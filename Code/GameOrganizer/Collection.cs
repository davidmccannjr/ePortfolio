using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using System.Xml;

namespace GameOrganizer
{
    public partial class Collection : Form
    {
        #region Variables / Properties

        /// <summary>
        /// Default color of a selected control.
        /// </summary>
        private static Color DefaultGameControlColor
        {
            get { return Color.LightBlue; }
        }

        /// <summary>
        /// Background color of a selected control.
        /// </summary>
        private static Color HighlightGameControlColor
        {
            get { return Color.White; }
        }

        /// <summary>
        /// Returns a string of the location of the base directory
        /// </summary>
        public static string BaseDirectory
        {
            get { return AppDomain.CurrentDomain.BaseDirectory; }
        }

        /// <summary>
        /// Name for the catch-all container that stores the games that can't
        /// be catagorized in other containers due to special cases.
        /// </summary>
        private static string CatchAllContainerName
        {
            get { return "%"; }
        }

        /// <summary>
        /// Has the collection list been altered?
        /// Used to request save if true.
        /// </summary>
        private bool collectionChanged;

        /// <summary>
        /// Stats relating to the game collection.
        /// </summary>
        private readonly CollectionStats stats;

        /// <summary>
        /// Currently selected game control.
        /// </summary>
        private GameControl selected;

        /// <summary>
        /// List of container controls based on the sort condition.
        /// Add containers using the AddContainer function.
        /// </summary>
        private List<ContainerControl> containers;

        /// <summary>
        /// Master list of games in the collection. 
        /// Add games using the AddGame function.
        /// </summary>
        private List<Game> games;

        /// <summary>
        /// Save path for the collection.
        /// </summary>
        private string collectionPath;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor for a collection form.
        /// </summary>
        public Collection()
        {
            InitializeComponent();

            collectionPath = "";
            stats = new CollectionStats();
            games = new List<Game>();
            containers = new List<ContainerControl>();

            // Adds function that disables scroll wheel input on the sort combo box
            c_sort.MouseWheel += sortCombo_MouseWheelMove;

            // Add function to be called when the user changes how the games are sorted.
            c_sort.SelectedIndex = 0;
            c_sort.SelectedIndexChanged += sortCombo_IndexChange;

            // Disable the edit and delete buttons - no game selected.
            c_edit.Enabled = c_delete.Enabled = false;                 
             
            // Adds delegate to the form closing event
            FormClosing += Collection_FormClosing;
        }

        #endregion

        #region Add / Remove

        /// <summary>
        /// Adds a container to both the container list and the
        /// list of controls for the container panel.
        /// </summary>
        /// <param name="name">Name of the container.</param>
        /// <param name="toSort">Game to add to the container.</param>
        private void AddContainer(string name, Game toSort)
        {
            // Create a new container control and add event listeners
            ContainerControl c = new ContainerControl(name);
            c.DropDownChange += container_OnDropChange;
            c.gameListPanel.ControlAdded += container_GameControlAdded;

            // Add the game to the container
            c.AddGame(toSort);

            // Add container to container list and as a control for the container panel.
            int index = containers.Count;
            for (int i = 0; i < containers.Count; i++)
            {
                int compareResult;
                try
                {   // Try comparing ints - Higher numbers ordered first
                    compareResult = -int.Parse(c.Name).CompareTo(int.Parse(containers[i].Name));
                }
                catch
                {
                    // Compare string - Lower letters ordered first
                    compareResult = String.Compare(c.Name, containers[i].Name);
                }
                
                if (compareResult == -1)
                {
                    index = i;
                    break;
                }
            }
            containers.Insert(index, c);
            containerListPanel.Controls.Add(c);
            containerListPanel.Controls.SetChildIndex(c, index);
        }

        /// <summary>
        /// Adds a game to the collection. A game control is then added to the
        /// container that matches the sort condition. If a container can not 
        /// be found, a new one is created.
        /// </summary>
        /// <param name="toAdd">Game to add.</param>
        public void AddGame(Game toAdd)
        {
            if (toAdd == null) return;

            // Do not add games with similar name and platform
            if (games.Contains(toAdd))
            {
                MessageBox.Show(String.Format(
                    "'{0}' for {1} is already in collection.", 
                    toAdd.Title, 
                    toAdd.Platform));
                return;
            }

            // Insert game to the sorted game list based on the game's title
            int index = games.Count;
            for (int i = 0; i < games.Count; i++)
            {
                if (String.Compare(toAdd.Title, games[i].Title) == -1)
                {
                    index = i;
                    break;
                }
            }
            games.Insert(index, toAdd);
            stats.GameAdded(toAdd);

            // Adds game to the appropriate container
            GetSortingFunc()(toAdd);

            // Reallign containers to account for newly added game
            ReallignContainers();
        }

        /// <summary>
        /// Removes all containers.
        /// </summary>
        private void RemoveAll()
        {
            while (containers.Count != 0)
            {
                RemoveContainer(containers[0]);
            }
        }

        /// <summary>
        /// Removes a container and removes the games stored from the collection.
        /// </summary>
        private void RemoveContainer(ContainerControl c)
        {
            // Remove all games from container/*
            while (c.gameControls.Count != 0)
            {
                RemoveGame(c.gameControls[0]);
            }

            // Remove delegates from container
            c.DropDownChange -= container_OnDropChange;
            c.gameListPanel.ControlAdded -= container_GameControlAdded;

            // Remove container from application
            containers.Remove(c);
            containerListPanel.Controls.Remove(c);
            c.Dispose();
        }

        /// <summary>
        /// Removes the game from the collection. 
        /// </summary>
        /// <param name="toRemove">Game to remove.</param>
        private void RemoveGame(GameControl toRemove)
        {
            if (toRemove == null) return;

            // Reset selected value if the game being removed is the selected control
            if (selected == toRemove)
            {
                ResetSelected();
            }

            // Find container and remove the game from it
            ContainerControl c = containers.Find(a => a.gameControls.Contains(toRemove));
            if (c != null)
            {
                c.RemoveGame(toRemove);
                if (c.gameControls.Count == 0)
                {
                    RemoveContainer(c);
                }
            }

            // Remove delegate from click event
            toRemove.Click -= container_GameSelected;

            // Remove game from the collection
            games.Remove(toRemove.Game);
            stats.GameRemoved(toRemove.Game);
            ReallignContainers();
        }

        /// <summary>
        /// Displays a save file dialog and saves collection to a text file.
        /// </summary>
        private bool Save()
        {
            if(collectionPath == "" ||
                System.IO.Path.GetExtension(collectionPath) != ".xml")
            {
                // Create the save file dialog
                SaveFileDialog s = new SaveFileDialog();
                s.Filter = "XML File (*.xml) | *.xml";
                s.OverwritePrompt = true;

                if(s.ShowDialog() == DialogResult.OK)
                {
                    // Enforce proper extension
                    string ext = System.IO.Path.GetExtension(s.FileName);
                    if (ext != ".xml")
                    {
                        s.FileName.Replace(ext, ".xml");
                    }
                    collectionPath = s.FileName;
                }
                else
                {
                    return false;
                }
            }

            // Save file asks for overwrite, so delete file at path
            System.IO.File.Delete(collectionPath);

            var write = XmlWriter.Create(collectionPath);
            write.WriteStartDocument();
            write.WriteStartElement("Collection");
            foreach(Game game in games)
            {
                write.WriteStartElement("Game");
                write.WriteElementString("Title", game.Title);
                write.WriteElementString("Platform", game.Platform);
                write.WriteElementString("Genre", game.Genre);
                write.WriteElementString("Release", game.Released.ToShortDateString());
                write.WriteElementString("Purchase", game.Purchased.ToShortDateString());
                write.WriteElementString("Cost", game.Cost.ToString("0.00"));
                write.WriteElementString("Rating", game.Rating.ToString());
                write.WriteElementString("HasCover", game.HasManual.ToString());
                write.WriteElementString("HasManual", game.HasManual.ToString());
                write.WriteElementString("Cover", game.CoverFilePath);
                write.WriteEndElement();

            }
            write.WriteEndElement();
            write.WriteEndDocument();
            write.Close();

            collectionChanged = false;
            return true;
        }

        #endregion

        #region Sorting

        /// <summary>
        /// Gets the function that organizes the games
        /// based on how the games are to be sorted.
        /// </summary>
        /// <returns>Sorter function</returns>
        public Action<Game> GetSortingFunc()
        {
            switch (c_sort.Text)
            {
                case "Platform":
                    return SortByPlatform;
                case "Genre":
                    return SortByGenre;
                case "Release Date":
                    return SortByRelease;
                case "Purchase Date":
                    return SortByPurchase;
                case "Cost":
                    return SortByCost;
                case "Rating":
                    return SortByRating;
                case "Title":
                default:
                    break;
            }

            return SortByTitle;
        }

        /// <summary>
        /// Adds a game to the display based on the first letter in the title.
        /// </summary>
        /// <param name="toSort">Game to sort</param>
        public void SortByTitle(Game toSort)
        {
            // Use the first letter in the title to organize the games
            string first = toSort.Title.Substring(0, 1).ToUpper();

            // Look to see if an appropriate container already exists
            bool hasContainer = false;
            ContainerControl c = containers.Find(a => a.Name.Substring(0, 1) == first);
            if (c != null)
            {
                c.AddGame(toSort);
                hasContainer = true;
            }
            
            // If not, create a new container to store the game.
            if (!hasContainer)
            {
                if (Char.IsLetter(first, 0))
                {
                    AddContainer(first, toSort);
                }
                else
                {
                    // Find catch-all container
                    c = containers.Find(a => a.Name == CatchAllContainerName);
                    if (c == null)
                    {
                        AddContainer(CatchAllContainerName, toSort);
                    }
                    else
                    {
                        c.AddGame(toSort);
                    }
                }
            }
        }

        /// <summary>
        /// Adds a game to the display based on the platform.
        /// </summary>
        /// <param name="toSort">Game to sort</param>
        public void SortByPlatform(Game toSort)
        {
            // Look to see if an appropriate container already exists
            bool hasContainer = false;
            ContainerControl c = containers.Find(a => a.Name == toSort.Platform);
            if (c != null)
            {
                c.AddGame(toSort);
                hasContainer = true;
            }

            // If not, create a new container to store the game
            if (!hasContainer)
            {
                AddContainer(toSort.Platform, toSort);
            }
        }

        public void SortByGenre(Game toSort)
        {
            // Look to see if an appropriate container already exists
            bool hasContainer = false;
            ContainerControl c = containers.Find(a => a.Name == toSort.Genre);
            if (c != null)
            {
                c.AddGame(toSort);
                hasContainer = true;
            }

            // If not, create a new container to store the game
            if (!hasContainer)
            {
                AddContainer(toSort.Genre, toSort);
            }
        }

        public void SortByRelease(Game toSort)
        {
            // Look to see if an appropriate container already exists
            bool hasContainer = false;
            ContainerControl c = containers.Find(a => a.Name == toSort.Released.Year.ToString());
            if (c != null)
            {
                c.AddGame(toSort);
                hasContainer = true;
            }

            // If not, create a new container to store the game
            if (!hasContainer)
            {
                AddContainer(toSort.Released.Year.ToString(), toSort);
            }
        }

        public void SortByPurchase(Game toSort)
        {
            // Look to see if an appropriate container already exists
            bool hasContainer = false;
            ContainerControl c = containers.Find(a => a.Name == toSort.Purchased.Year.ToString());
            if (c != null)
            {
                c.AddGame(toSort);
                hasContainer = true;
            }

            // If not, create a new container to store the game
            if (!hasContainer)
            {
                AddContainer(toSort.Purchased.Year.ToString(), toSort);
            }
        }

        public void SortByCost(Game toSort)
        {
            // Get range of values
            double cost = Convert.ToDouble(toSort.Cost);
            double floor = cost <= 0 ? 0 : Math.Floor(cost / 10) * 10;
            double ceil = cost <= 0 ? 9.99: Math.Ceiling(cost / 10) * 10 - 0.01;
            string range = "$" + floor + " - $" + ceil;

            // Look to see if an appropriate container already exists
            bool hasContainer = false;
            ContainerControl c = containers.Find(a => a.Name == range);
            if (c != null)
            {
                c.AddGame(toSort);
                hasContainer = true;
            }

            // If not, create a new container to store the game
            if (!hasContainer)
            {
                AddContainer(range, toSort);
            }
        }

        public void SortByRating(Game toSort)
        {
            // Look to see if an appropriate container already exists
            bool hasContainer = false;
            ContainerControl c = containers.Find(a => a.Name == toSort.Rating.ToString());
            if (c != null)
            {
                c.AddGame(toSort);
                hasContainer = true;
            }

            // If not, create a new container to store the game
            if (!hasContainer)
            {
                AddContainer(toSort.Rating.ToString(), toSort);
            }
        }

        /// <summary>
        /// Realligns the containers to place them in their organized positions.
        /// </summary>
        private void ReallignContainers()
        {
            // FASTER IMPLEMENT 
            //*
            Point p = new Point(0, -containerListPanel.AutoScrollPosition.Y);
            containerListPanel.AutoScrollPosition = Point.Empty;

            int y = 0;
            for (int i = 0; i < containerListPanel.Controls.Count; i++)
            {
                containerListPanel.Controls[i].Location = new Point(0, y);
                y += ((ContainerControl)containerListPanel.Controls[i]).DisplayHeight;
            }

            containerListPanel.AutoScrollPosition = p;

           
        }

        #endregion // Sort Methods

        #region Selected

        /// <summary>
        /// Unselects the current selected game and resets display information.
        /// </summary>
        public void ResetSelected()
        {
            // Set background color back to default
            if (selected != null)
            {
                selected.BackColor = DefaultGameControlColor;
            }

            // Reset all display to default
            c_title.ResetText();
            c_platform.ResetText();
            c_genre.ResetText();
            c_releaseDate.ResetText();
            c_purchaseDate.ResetText();
            c_cost.ResetText();
            c_ratingControl.SetRating(0);
            checkBox1.Checked = checkBox2.Checked = false;
            c_cover.Image = null;

            // Remove selected reference
            selected = null;

            // Disable buttons to edit or delete the selected game
            c_edit.Enabled = c_delete.Enabled = false;
        }

        /// <summary>
        /// Unselects the current selected game and resets display information.
        /// </summary>
        public void SetSelected(GameControl gC)
        {
            // If there is a selected, change background color of selected to default
            if (selected != null)
            {
                selected.BackColor = DefaultGameControlColor;
            }

            // Set selected to the new game control
            selected = gC;
            selected.BackColor = HighlightGameControlColor;

            // Set selected to the game passed in event argument
            c_cover.Image = selected.Game.CoverArt;
            if (selected.Game.CoverFilePath != "" && c_cover.Image == null)
            {   // Error loading image
                c_cover.Image = null;
                c_cover.BackgroundImage = System.Drawing.SystemIcons.Error.ToBitmap();
            }

            // Display selected game information
            c_title.Text = selected.Game.Title;
            c_platform.Text = selected.Game.Platform;
            c_genre.Text = selected.Game.Genre;
            c_releaseDate.Text = selected.Game.Released.ToShortDateString();
            c_purchaseDate.Text = selected.Game.Purchased.ToShortDateString();
            c_cost.Text = selected.Game.Cost.ToString("$0.00");
            c_ratingControl.SetRating(selected.Game.Rating);
            checkBox1.Checked = selected.Game.HasCover;
            checkBox2.Checked = selected.Game.HasManual;

            // Enable buttons to edit or delete selected game
            c_edit.Enabled = c_delete.Enabled = true;
        }

        #endregion

        #region Events

        /// <summary>
        /// Delegate called when clicking on the add button.
        /// Displays a form where the user enters the new object information.
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event args</param>
        private void addButton_Click(object sender, EventArgs e)
        {
            // Display game form and created add game
            using (GameForm g = new GameForm())
            {
                if (g.ShowDialog() == DialogResult.OK)
                {
                    collectionChanged = true;
                    AddGame(g.Game);
                }
            }
        }

        /// <summary>
        /// Checks to see if changes have been made to the form.
        /// If there have been, request if user wanted to save these changes.
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Closing event args</param>
        void Collection_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Show dialog to request a save if changes have been made
            if (collectionChanged)
            {
                DialogResult result = MessageBox.Show(
                    "Save changes before closing?",
                    "Save Changes?",
                    MessageBoxButtons.YesNoCancel);

                if (result == DialogResult.Yes)
                {
                    if(!Save())
                    {
                        e.Cancel = true;
                    }
                }
                else if (result == DialogResult.Cancel)
                {
                    // Cancel close operation
                    e.Cancel = true;
                }
            }
        }

        /// <summary>
        /// Delegate called when a game control is added to a container.
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Game Control args</param>
        private void container_GameControlAdded(object sender, ControlEventArgs e)
        {
            GameControl gC = e.Control as GameControl;

            if (gC == null) return;

            // Add delegate to the click event
            gC.Click += container_GameSelected;
        }

        /// <summary>
        /// Delegate called when a game control is selected.
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Game Control args</param>
        private void container_GameSelected(object sender, EventArgs e)
        {
            GameControl gC = sender as GameControl;

            if (gC == null) return;

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
        /// Delegate called when a container dropdown is clicked.
        /// Realligns the containers on the container list display.
        /// </summary>
        /// <param name="sender">Object sender</param>
        /// <param name="e">Event args</param>
        private void container_OnDropChange(object sender, EventArgs e)
        {
            ReallignContainers();
        }

        /// <summary>
        /// Delegate called when the delete button is clicked.
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event args</param>
        private void deleteButton_Click(object sender, EventArgs e)
        {
            if (selected == null) return;

            RemoveGame(selected);
            collectionChanged = true;
        }

        /// <summary>
        /// Delegate called when clicking on the edit button.
        /// Displays a form where the user can change the object information.
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event args</param>
        private void editButton_Click(object sender, EventArgs e)
        {
            if (selected == null) return;

            // Create game form with the selected game as a template
            // Remove selected game and add newly created game
            GameForm form = new GameForm(selected.Game);
            if (form.ShowDialog() == DialogResult.OK)
            {
                RemoveGame(selected);
                AddGame(form.Game);
                collectionChanged = true;

                // Find game control and select it
                GameControl gc = containers.SelectMany(a => a.gameControls).
                    Where(b => b.Game == form.Game).
                    Single();
                SetSelected(gc);
            }
        }

        /// <summary>
        /// Delegate called when the new tool strip menu item is clicked.
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event args</param>
        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Show dialog to request a save if changes have been made
            if(collectionChanged)
            {
                DialogResult result = MessageBox.Show(
                    "Save changes before continuing?",
                    "Save Changes?",
                    MessageBoxButtons.YesNoCancel);

                if(result == DialogResult.Yes)
                {
                    if (!Save())
                    {
                        return;
                    }
                }
                else if(result == DialogResult.Cancel)
                {
                    return;
                }
            }

            // Remove all of the games in the collection
            collectionPath = "";
            collectionChanged = false;
            RemoveAll();
        }

        /// <summary>
        /// Delegate called when open menu item clicked.
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event args</param>
        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Request a save if changes have been made
            if (collectionChanged)
            {
                DialogResult result = MessageBox.Show(
                    "Save changes before continuing?",
                    "Save Changes?",
                    MessageBoxButtons.YesNoCancel);

                if (result == DialogResult.Yes)
                {
                    if (!Save())
                    {
                        return;
                    }
                }
                else if (result == DialogResult.Cancel)
                {
                    return;
                }
            }

            // Create an open file dialog to open a file
            OpenFileDialog o = new OpenFileDialog();
            o.Filter = "XML Files(*.xml) | *.xml";

            if (o.ShowDialog() == DialogResult.OK)
            {
                // Set new collection path
                collectionPath = o.FileName;

                // Remove current collection and open a saved one
                RemoveAll();

                var doc = XDocument.Load(o.FileName);
                foreach (var game in doc.Descendants("Game"))
                {
                    try
                    {
                        string title = game.Element("Title").Value;
                        string platform = game.Element("Platform").Value;
                        string genre = game.Element("Genre").Value;
                        DateTime release = Convert.ToDateTime(game.Element("Release").Value);
                        DateTime purchased = Convert.ToDateTime(game.Element("Purchase").Value);                        
                        double cost = Convert.ToDouble(game.Element("Cost").Value);
                        byte rating = Convert.ToByte(game.Element("Rating").Value);
                        bool hasCover = Convert.ToBoolean(game.Element("HasCover").Value);
                        bool hasManual = Convert.ToBoolean(game.Element("HasManual").Value);
                        string location = game.Element("Cover").Value;
                        AddGame(Game.Build(GameInfo.Build(title, platform, genre, release, location),
                            new OwnershipInfo(purchased, cost, hasCover, hasManual, rating)));
                    }
                    catch { continue; }
                }

                // New collection opened
                collectionChanged = false;
            }
        }

        /// <summary>
        /// Saves the collection as a new file.
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event args</param>
        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string temp = collectionPath;
            collectionPath = "";
            if (!Save())
            {
                collectionPath = temp;
            }
        }

        /// <summary>
        /// Saves the collection.
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event args</param>
        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Save();
        }

        /// <summary>
        /// Event called when the game sorting logic is changed.
        /// Organizes the game collection based on the new sorting logic.
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event args</param>
        private void sortCombo_IndexChange(object sender, EventArgs e)
        {
            // Remove all container information
            containerListPanel.AutoScrollPosition = Point.Empty;
            containerListPanel.Controls.Clear();
            containers.Clear();

            // Reset selected game information
            ResetSelected();

            // Sort games into new containers using the sorting function
            Action<Game> sorter = GetSortingFunc();
            for (int i = 0; i < games.Count; i++)
            {
                sorter(games[i]);
            }

            // Reallign new containers
            ReallignContainers();
        }

        /// <summary>
        /// Event called when the sort combo box recieves mouse scrollwheel input.
        /// Disables any mouse scrollwheel input.
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event args</param>
        private void sortCombo_MouseWheelMove(object sender, EventArgs e)
        {
            HandledMouseEventArgs ee = (HandledMouseEventArgs)e;
            ee.Handled = true;
        }

        /// <summary>
        /// Displays the collection stats form.
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event args</param>
        private void viewCollectionStatsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            stats.ShowDialog();
        }

        /// <summary>
        /// Creates a form to display all of the games in the collection.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void viewGamesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Create new form
            Form gameListForm = new Form();
            gameListForm.FormBorderStyle = FormBorderStyle.FixedToolWindow;
            gameListForm.StartPosition = FormStartPosition.CenterScreen;
            gameListForm.Size = new Size(400, 300);

            // Create list view to display games
            ListView list = new ListView();
            gameListForm.Controls.Add(list);

            list.Dock = DockStyle.Fill;
            list.View = View.Details;
            list.FullRowSelect = true;
            list.Columns.Add("Title", (int)(list.Size.Width * (3 / 4.0)));
            list.Columns.Add("Platform", list.Width - list.Columns[0].Width);

            // Add games to the list view
            for (int i = 0; i < games.Count; i++)
            {
                ListViewItem item = new ListViewItem(new string[] { games[i].Title, games[i].Platform });
                list.Items.Add(item);
            }

            // Display the form
            gameListForm.ShowDialog();
        }

        #endregion // EventHandler Methods
    }
    
}