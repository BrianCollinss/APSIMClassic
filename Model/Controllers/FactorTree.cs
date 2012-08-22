using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Diagnostics;
using System.Windows.Forms;
using System.Xml;
using System.Collections.Specialized;
namespace Controllers
{

	public partial class FactorTree : TreeView
	{

		private BaseController Controller;

		private bool FirstTimeRename = false;
			//create a new Context Menu for the page [right click anywhere on the page]
		private System.Windows.Forms.ContextMenuStrip PopupMenu = new System.Windows.Forms.ContextMenuStrip();
			//create a new Context Menu for a drag using the right mouse button. See TreeView Drag Events below
		private System.Windows.Forms.ContextMenuStrip PopupMenuRightDrag = new System.Windows.Forms.ContextMenuStrip();


        public FactorTree(System.ComponentModel.IContainer container)
            : this()
        {
            //Required for Windows.Forms Class Composition Designer support
            if ((container != null))
            {
                container.Add(this);
            }
        }

        public FactorTree()
            : base()
        {
            DragDrop += TreeView_DragDrop;
            DragOver += TreeView_DragOver;
            ItemDrag += TreeView_ItemDrag;
            AfterLabelEdit += OnAfterEdit;
            BeforeLabelEdit += OnBeforeEdit;
            MouseDown += TreeView_MouseDown;

            //This call is required by the Component Designer.
            InitializeComponent();
        }

        public void OnLoad(BaseController Controller)
		{
			// ---------------------------------------------------
			// Set ourselves up.
			// ---------------------------------------------------

			Controller.ApsimData.ComponentChangedEvent += OnRefresh;
			//        AddHandler Controller.ApsimData.FileNameChanged, AddressOf OnFileNameChanged
			Controller.FactorialSelectionChangedEvent += OnSelectionChanged;

			PathSeparator = "/";
			ImageList = Controller.ImageList("SmallIcon");

			this.ShowNodeToolTips = true;
			this.Controller = Controller;
			this.ContextMenuStrip = PopupMenu;
			//tell the datatree that its context menu is the popup menu
			Controller.ProvideToolStrip(PopupMenu, "FactorContextMenu");
			//initialise Context Menu for the page using the controller [popup when you right click anywhere on the page]

			//initialise Context Menu for the right mouse button drag [popup when you drop the node you were dragging with the right mouse]
			PopupMenuRightDrag.Items.Add("Copy Here");
			PopupMenuRightDrag.Items.Add("Move Here");
			PopupMenuRightDrag.Items.Add("Create Link Here");
			PopupMenuRightDrag.Items.Add(new ToolStripSeparator());
			PopupMenuRightDrag.Items.Add("Cancel");

		}

		#region "Refresh methods"
		private void OnRefresh(ApsimFile.Component Comp)
		{
			//This should only happen if the user  opens a file /or new file that doesn't have a Factorial Component
			if ((Controller.ApsimData.FactorComponent == null) && Controller.FactorialMode) {
				Controller.FactorialMode = false;
				return;
			}

			// ----------------------------------------------
			// Do a refresh from the specified Comp down
			// ----------------------------------------------
			System.Windows.Forms.Cursor.Current = Cursors.WaitCursor;
			//set the cursor object (usually an arrow) to the wait cursor (usually an hourglass)
			BeginUpdate();
			//inbuilt tree function, it disables redrawing of the tree

			try {
				if (Controller.ApsimData.FactorComponent == null) {
					Nodes.Clear();
					//get rid of all the nodes in the tree 
				} else {
					//If (the tree has no nodes) OR (Comp [the component parameter this sub was passed] is Null)
					if ((Nodes.Count == 0) || (Comp == null)) {
						Nodes.Clear();
						//get rid of all the nodes in the tree 
						TreeNode RootNode = Nodes.Add(Controller.ApsimData.FactorComponent.Name);
						//create the root node from the root component and add it to the tree.
						RefreshNodeAndChildren(RootNode, Controller.ApsimData.FactorComponent);
						//refresh the tree from the root node down.
					} else {
						TreeNode NodeToRefresh = GetNodeFromPath(Comp.FullPath);
						//get the corresponding node for the component this sub was passed
						//return the corresponding component to the node that was selected.
						//if you have switched from one toolbox to another toolbox, then even though the components exist to do the refresh, the corresponding nodes do not yet exist because this OnRefresh is supposed to provide them. So GetNodeFromPath will return Nothing.
						if ((NodeToRefresh == null)) {
							RefreshNodeAndChildren(Nodes[0], Controller.ApsimData.FactorComponent);
							//refresh the tree from this node down.
						} else {
							RefreshNodeAndChildren(NodeToRefresh, Comp);
							//refresh the tree from this node down.
						}
					}
				}
			} catch (Exception) {
				EndUpdate();
				//inbuilt tree function, reinables redrawing of the tree
				System.Windows.Forms.Cursor.Current = Cursors.Default;
				//set the cursor object back to the default windows cursor (usually an arrow)
				throw;
			}

			EndUpdate();
			//inbuilt tree function, reinables redrawing of the tree
			System.Windows.Forms.Cursor.Current = Cursors.Default;
			//set the cursor object back to the default windows cursor (usually an arrow)
		}
		private void RefreshNodeAndChildren(TreeNode Node, ApsimFile.Component Comp)
		{
			// --------------------------------------------------
			// Recursively refresh the specified treenode and its
			// child nodes in the tree.
			// --------------------------------------------------

			// Refresh the specified node first.
			Node.Text = Comp.Name;
			Node.ImageIndex = Controller.ImageIndex(Comp.Type, "SmallIcon");
			Node.SelectedImageIndex = Node.ImageIndex;
			Node.Tag = Comp.Type;
			Node.ToolTipText = Comp.Description;

			if ((Comp.ShortCutTo != null)) {
				Node.ToolTipText = "Linked to " + Comp.ShortCutTo.FullPath;
				if (!Comp.Enabled) {
					Node.ToolTipText = "Disabled: " + Node.ToolTipText;
				}
			}
			if (!Comp.Enabled) {
				Node.ToolTipText = "Disabled" + Node.ToolTipText;
			}
			ColourNode(Node);
			// Go refresh all children.
			int ChildIndex = 0;
			foreach (ApsimFile.Component Child in Comp.ChildNodes) {
				TreeNode ChildTreeNode = null;
				if (ChildIndex < Node.Nodes.Count) {
					ChildTreeNode = Node.Nodes[ChildIndex];
				} else {
					ChildTreeNode = Node.Nodes.Add(Child.Name);
				}
				RefreshNodeAndChildren(ChildTreeNode, Child);
				ChildIndex = ChildIndex + 1;
			}
			while (Node.Nodes.Count > ChildIndex) {
				Node.Nodes.Remove(Node.Nodes[Node.Nodes.Count - 1]);
			}
		}
		private TreeNode GetNodeFromPath(string ChildPath)
		{
			// --------------------------------------------------
			// Returns a tree node given a fullly delimited path.
			// --------------------------------------------------
			if (string.IsNullOrEmpty(ChildPath)) {
				return null;
			}
			//reomve RootComponent namfrom path (first part?)
			string name = "";
			string Path = ChildPath.Substring(1);
			int PosDelimiter = Path.IndexOf(PathSeparator);
			//Path will have the RootComponent in it which is not shown in the Factorial Tree
			if (PosDelimiter != -1) {
				Path = Path.Substring(PosDelimiter + 1);
			}

			TreeNode CurrentNode = null;
			while (!(string.IsNullOrEmpty(Path))) {
				PosDelimiter = Path.IndexOf(PathSeparator);
				if (PosDelimiter != -1) {
					name = Path.Substring(0, PosDelimiter);
					Path = Path.Substring(PosDelimiter + 1);
				} else {
					name = Path;
					Path = "";
				}

				TreeNode ChildNode = null;
				if (CurrentNode == null) {
					if (Nodes.Count == 0) {
						return null;
					} else {
						ChildNode = Nodes[0];
					}
				} else {
					foreach (TreeNode ChildNode_loopVariable in CurrentNode.Nodes) {
						ChildNode = ChildNode_loopVariable;
						if (ChildNode.Text.ToLower() == name.ToLower()) {
							break; // TODO: might not be correct. Was : Exit For
						}
					}
				}
				CurrentNode = ChildNode;
				if ((CurrentNode != null)) {
					if (CurrentNode.Text.ToLower() != name.ToLower()) {
						CurrentNode = null;
					}
				}
				if ((CurrentNode == null)) {
					break; // TODO: might not be correct. Was : Exit Do
				}
			}

			return CurrentNode;
		}
		private string GetPathFromNode(TreeNode Node)
		{
			return PathSeparator + Controller.ApsimData.RootComponent.Name + PathSeparator + Node.FullPath;
			//just put an extra "/" in front of the node path, so "root/child" becomes "/root/child" (this is needed because our "Selected Path" root starts with a /, whereas the inbuilt node full path property does not start with a / at the root)
			//Return Node.FullPath    'DO NOT put an extra "/" in front of the node path, so "root/child" becomes "/root/child" (this is needed because our "Selected Path" root starts with a /, whereas the inbuilt node full path property does not start with a / at the root)
		}

        private void ColourNode(TreeNode Node)
		{

		    Font LinkFont = new System.Drawing.Font(this.Font.FontFamily, this.Font.Size, FontStyle.Underline);
		    Font UnLinkFont = new System.Drawing.Font(this.Font.FontFamily, this.Font.Size, FontStyle.Regular);

			//NB. Windows inbuilt Tree control does not allow you to select more then one node.
			//   So we had to create our own code to manually highlight the nodes that are selected. (manually changed the background/foreground properties of each node in the tree) (see the code below) 
			//   This is also why we needed to create the SelectedPaths property in the Base Control, instead of just using the tree controls inbuilt SelectedNode property.

			//If the node is linked to another node  
			//nb. ToolTipText is the text that appears when you hover the mouse over the node. IndexOf just returns the index of the first occurance of one string in another.
			if (Node.ToolTipText.IndexOf("Linked to") == 0) {
				Node.ForeColor = Color.Blue;
				//colour to blue
				Node.NodeFont = LinkFont;
				//font to underlined (see LinkFont variable declared just above this ColourNode function)
				Node.BackColor = BackColor;
				//back colour to default back colour for the tree

			//If the node is disabled 

			} else if (Node.ToolTipText.IndexOf("Disabled") == 0) {
				Node.ForeColor = SystemColors.InactiveCaptionText;
				//colour to the default colour for a windows system disabled element 
				Node.BackColor = SystemColors.InactiveCaption;
				//back colour to the default back colour for a disabled item in windows 

			//If it's just a normal node
			} else {
				Node.ForeColor = Color.Black;
				//colour to black
				Node.BackColor = BackColor;
				//back colour to default back colour for the tree
				Node.NodeFont = UnLinkFont;
				//font to regular (see UnLinkFont variable declared just above this ColourNode function)


			}

			//If the node is a selected node
			//this IndexOf is for a string collection NOT a string. So it it checks every string in the collection for an exact match with the search string. If it finds one it returns that strings index in the collection.
			if (Controller.SelectedPaths.IndexOf(GetPathFromNode(Node)) != -1) {
				Node.ForeColor = SystemColors.HighlightText;
				//colour to the default colour for a selected item in windows
				Node.BackColor = SystemColors.Highlight;
				//back colour to the default back colour for a selected item in windows

			}
		}
		#endregion

		#region "Selection Methods"
		private void TreeView_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			// ---------------------------------------------------------------
			// If the user right clicks on a node and that node isn't already
			// selected, then go select it.
			// ---------------------------------------------------------------

			//Don't let the user click any other nodes while this code executes

			//Initialise variables
			TreeNode ClickedNode = GetNodeAt(e.Location);
			//get the node in the datatree that was clicked on.
			//if the button was pressed on a non node area of the datatree
			if ((ClickedNode == null)) {
				return;
				//do nothing.
			}

			//allow for an image 20 pixels to the left of the text of a node
			if ((e.X < ClickedNode.Bounds.Left - 20)) {
				return;
			}

			bool LeftClick = (e.Button == System.Windows.Forms.MouseButtons.Left);
			//is it a right mouse button click
			string SelectedPath = GetPathFromNode(ClickedNode);

			//Right Click Only           
			if (LeftClick) {
				//Left Click Only
				//if user clicked on a node and not a blank area (was tested for null earlier)
				//if the user clicked again on the same thing that was already selected -> then do a Rename.
				//click on same thing that was already selected.

				if (!Controller.ApsimData.IsReadOnly && SelectedPath == Controller.SelectedFactorialPath && ClickedNode.Level > 0) {
					//if not readonly, 
					//and user has clicked once before, 
					//and what they clicked this time is the same as what they clicked last time, 
					//and they have clicked on a node that is lower down then the root node [can't rename the root node] 

					LabelEdit = true;
					//set the tree's label edit property  to true, allowing all the nodes on the tree to have their labels edited. (needs to be set to true for Node.BeginEdit() to work)  
					FirstTimeRename = true;
					ClickedNode.BeginEdit();
					//call the inbuilt tree node function that allows the user to edit the nodes label. (see OnBeforeEdit and OnAfterEdit sub for what happens before and after the user edits the label)
					return;
				}
			}

			//if they clicked on something different to what was already selected -> change the selection
			//Finish off

			Controller.SelectedFactorialPath = SelectedPath;
			//update the selected paths "in the controller"

		}
		private void OnSelectionChanged(string OldSelection, string NewSelection)
		{
			// -----------------------------------------------------------------
			// Selection has changed - update tree.
			// -----------------------------------------------------------------
			//don't let the user click any other nodes while this code executes

			//Change the colour of all the old selected nodes to the "unselected" colours
			TreeNode Node = GetNodeFromPath(OldSelection);
			//get the node that the old selected path points to
			if ((Node != null)) {
				ColourNode(Node);
				//change the colour of the unselected node to the unselected colours.
			}

			//Change the colour of all the new selected nodes to the "selected" colours
			//get the node that the new selected path points to.
			SelectedNode = GetNodeFromPath(NewSelection);
			//set the Tree's selected node to the node specified in the new selected path (just used to trigger the AfterSelect event, which is handled by OnTreeSelectionChanged() subroutine below this subroutine) (nb. we REDO this for EVERY node in NewSelections. We have to do this one node at a time because the Tree does not allow you to select more then one node) 
			if ((SelectedNode != null)) {
				ColourNode(SelectedNode);
				//change the colour of the new selected node to the selected colours.
				SelectedNode.EnsureVisible();
				//use inbuilt tree node function that expands the tree to make sure the node specified is visible in the tree.  
				SelectedNode.EnsureVisible();
				//use inbuilt tree node function that expands the tree to make sure the node specified is visible in the tree.  
			}

			//let the user click on other nodes again
		}
		#endregion

		#region "Rename methods"        'This is a Rename done by doing 2 seperate clicks (See "Left Click Only" code in TreeView_MouseDown). NOT by a right mouse click then selecting rename, this is handled by Rename() sub in BaseAction.vb
		//event handlers for a Node.BeginEdit()
		private void OnBeforeEdit(object sender, System.Windows.Forms.NodeLabelEditEventArgs e)
		{
			// ---------------------------------------------------
			// User is about to start editing a tree node.
			// We must disable the popup menu because if the user
			// hits DELETE while editing the node, the ACTION
			// will trigger, deleting the whole node rather than
			// the bit of text on the node caption.
			// ---------------------------------------------------
			this.ContextMenuStrip.Enabled = false;

		}
		private void OnAfterEdit(object sender, System.Windows.Forms.NodeLabelEditEventArgs e)
		{
			// ---------------------------------------------------
			// User has just finished editing the label of a node.
			// ---------------------------------------------------
			if (!FirstTimeRename) {

				if ((e.Label != null)) {
					//Check user typed something in. So you are not trying to rename it to a blank.
					if ((e.Label.Length > 0)) {


						if (!(CSGeneral.Utility.CheckForInvalidChars(e.Label))) {
							// Firstly empty the current selections.
							Controller.SelectedFactorialPath = "";

							// Change the data
							ApsimFile.Component Comp = Controller.ApsimData.Find(GetPathFromNode(e.Node));
							Comp.Name = e.Label;

							// Now tell the base controller about the new selections.
							Controller.SelectedFactorialPath = Comp.FullPath;


						} else {
							MessageBox.Show("You can not use characters such as < > / \\ ' \" ` : ? | * & = ! . or space in the name");
							e.CancelEdit = true;
							//cancel the edit event.

						}

					} else {
						e.CancelEdit = true;
						//cancel the edit event.
					}
				}
				LabelEdit = false;
			}
			FirstTimeRename = false;
			this.ContextMenuStrip.Enabled = true;
		}
		#endregion

		#region "Drag / Drop methods"       'These events handle a drag with both the left mouse button and the right mouse button

		//Global variables for Drag/Drop methods
			//used to store the paths for all the components that have been selected in the drag
		protected StringCollection PathsBeingDragged;
			//is the drag event a drag using the right mouse button

        private void TreeView_ItemDrag(object sender, System.Windows.Forms.ItemDragEventArgs e)
		{
			// -----------------------------------------------------------------
			// User has initiated a drag on a node - the full xml of the node
			// is stored as the data associated with the drag event args.
			// -----------------------------------------------------------------

			//If what is being dragged is not already in the controller as a selected item.

			if ((Controller.SelectedFactorialPath != GetPathFromNode((TreeNode)e.Item))) {
				SelectedNode = (TreeNode)e.Item;
				//add it to the base controller (by setting the selected node of the tree to the dragged item. This then fires the selection changed event for the tree which I think is handled by the base controller. This will add the dragged items to the base controller)
			}

			//Work out the xml of what you are dragging.
			string FullXML = "";
			//used to store the xml of ALL the components that have been selected in the drag  'reset it to nothing, ready for recreation.
			ApsimFile.Component Comp = Controller.ApsimData.Find(Controller.SelectedFactorialPath);
			//get the component for this particular selected node (using it's path)
			FullXML = FullXML + Comp.FullXML();
			//get the xml for the component and add it to the xml of previous selected nodes
			PathsBeingDragged = new StringCollection();
			PathsBeingDragged.Add(Controller.SelectedFactorialPath);
			//store the paths of ALL the nodes that are being dragged in a global variable, so it can be used by other drag events.

			//Raise the other DragDropEvents
			DoDragDrop(FullXML, DragDropEffects.Copy);
			//parameters: (Store xml of what you are dragging in "data" Drag Event Argument), (allowable types of left mouse drags [Drag Drop Effects are of type FlagsAttribute, which allow bitwise operators AND and OR]). 

		}

		private void TreeView_DragOver(object sender, System.Windows.Forms.DragEventArgs e)
		{
			// --------------------------------------------------
			// User has dragged a node over us - allow drop?
			// --------------------------------------------------

			//Make sure you are actually dragging something
			//check the "data" Drag Event Argument
			if (e.Data.GetDataPresent(typeof(System.String))) {

				//If the mouse is currently dragging over a node and not over blank area
				Point pt = PointToClient(new Point(e.X, e.Y));
				//get the drop location
				TreeNode DestinationNode = GetNodeAt(pt);
				//find the node closest to the drop location

				if ((DestinationNode != null)) {
					//Work out the type of left drag this is (copy, move, create link/shortcut), and store it in the "Effect" Drag Event Argument
					string FullXML = (string)e.Data.GetData(DataFormats.Text);
					Debug.Print("drop = " + FullXML);

					ApsimFile.Component DropComp = Controller.ApsimData.Find(GetPathFromNode(DestinationNode));
					//get the corresponding component for the destination node.
					//   //If DropComp.AllowAdd(FullXML) Then                      'if allowed to drop this node onto this destination node

					//If Not IsNothing(PathsBeingDragged) AndAlso PathsBeingDragged.Count > 0 Then
					e.Effect = DragDropEffects.Copy;
				//End If
				//Else                                                    'if NOT allowed to drop this node onto this destination node
				//e.Effect = DragDropEffects.None                         'display circle with line through it symbol
				} else {
					e.Effect = DragDropEffects.None;
					//display circle with line through it symbol
				}
			}
		}

		private void TreeView_DragDrop(object sender, System.Windows.Forms.DragEventArgs e)
		{
			// --------------------------------------------------
			// User has released mouse button during a drag.
			// Accept the dragged node.
			// --------------------------------------------------

			//Get the Destination Node
			Point pt = ((TreeView)sender).PointToClient(new Point(e.X, e.Y));
			//get the drop location
			TreeNode DestinationNode = ((TreeView)sender).GetNodeAt(pt);
			//find the node closest to the drop location
			Controller.SelectedFactorialPath = GetPathFromNode(DestinationNode);
			//set the selected path for the controller to the path for the destination node

			//Get the xml of what was dragged.
			string FullXML = (string)e.Data.GetData(DataFormats.Text);
			//it was put into the "data" Drag Event Argument in the DoDragDrop call (in TreeViewDragItem event handler)  

			//Drag using the left mouse button

			//depending on the type of left mouse drag, do the corresponding drag action.
			switch (e.Effect) {
				//use the "Effect" Drag Event Argument to get the type of left mouse drag
				case DragDropEffects.Copy:
					//these DragDropEffects are just the changes to the mouse icon when you hover over a node whilst dragging
					DragCopy(FullXML);
					DestinationNode.Expand();
					break;
				default:
					//should not needed this, but just put it in to prevent errors.
					DragCancel();
					break;
			}
			if ((PathsBeingDragged != null)) {
				PathsBeingDragged = null;
				//clear the variable storing the dragged nodes 
			}
		}
		//Drag Actions

		//copy
		private void DragCopy(string FullXML)
		{
			//if this is a factor, then add dropped item intothe targets
			//this shoud be handled through events
			bool handled = false;
			StringCollection paths = PathsBeingDragged;

			if ((paths == null)) {
				string tmp = Parent.Name;
				paths = Controller.Explorer.DataTree.PathsBeingDragged;
				handled = Controller.Explorer.CurrentView.OnDropData(paths, FullXML);
			}

			if ((!handled)) {
				Controller.FactorialSelection.Add(FullXML);
				//add the dragged nodes to the destination node (destination node is stored as the current selection in the controller) 
			}
		}
		//cancel
		private void DragCancel()
		{
			//when you drag from datatree in simulation to datatree in a toolbox the PathsBeingDragged disappear and it causes an error. I have done this to avoid the error.
			if ((PathsBeingDragged != null)) {
				PathsBeingDragged = null;
				//clear the variable storing the dragged nodes 
			}
		}
		#endregion

	}
}
