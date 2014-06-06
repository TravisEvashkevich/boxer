using Boxer.Data;

namespace Boxer.Core
{
    public class Glue : MainViewModel
    {
        private static Glue _instance;
        public static Glue Instance
        {
            get { return _instance ?? (_instance = new Glue()); }
        }

        private bool _documentIsSaved = true;
        public bool DocumentIsSaved
        {
            get { return _documentIsSaved; }
            set
            {
                Set(ref _documentIsSaved, value);
            }
        }

        private Document _document;

        public Document Document
        {
            get { return _document; }
            set { Set(ref _document, value); }
        }

        protected override void InitializeCommands() { }
    }
}
