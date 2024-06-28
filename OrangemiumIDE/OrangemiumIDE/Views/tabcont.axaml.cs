using System.Collections.Generic;
using Avalonia.Controls;
using AvaloniaEdit.TextMate;
using TextMateSharp.Grammars;

namespace OrangemiumIDE.Views
{
    public partial class tabcont : UserControl
    {
        public string filepath = "";
        public TextMate.Installation tmi;
        public string langext = "";
		public Language? lang = null;
		public bool issaved = true;
        public List<docProb> problems = new();
        public tabcont()
        {
            InitializeComponent();
        }
    }
}
