namespace process_file_desc {
    class classFileEntry {
        public string FileName;
        public string FileSize;
        public string FileDate;
        public string FileDesc;
        public bool Extracted;

        public classFileEntry() {
            FileName = "";
            FileSize = "";
            FileDate = "";
            FileDesc = "";
            Extracted = false;
        }

        public classFileEntry(string fName, string fSize, string fDate, string fDesc) {
            FileName = fName;
            FileSize = fSize;
            FileDesc = fDesc;
            FileDate = fDate;
            Extracted = false;
        }
    }
}
