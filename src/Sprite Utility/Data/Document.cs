﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.Serialization;
using System.Windows.Forms;
using SpriteUtility.Data;
using SpriteUtility.Data.Export;

namespace SpriteUtility
{
    public class Document
    {
        private static bool m_TriggerInvalidate;
        private readonly ObservableCollection<Folder> _folders;
        private string m_Name;

        private static Document _instance;
        public static Document Instance
        {
            get { return _instance; }
        }

        public static void TryInvalidate(object sender, EventArgs e)
        {
            if (_instance != null)
                _instance.Invalidate(sender, e);
        }

        public Document()
        {
            Saved = false;
            FileName = "Not Saved";
            m_Name = "New Project";
            _folders = new ObservableCollection<Folder>();
            ImageData.ResetNames();
            m_TriggerInvalidate = true;
            DocumentChanged += OnDocumentChanged;
            DocumentSaved += OnDocumentSaved;
            NameChanged += OnNameChanged;
            _folders.CollectionChanged += FolderCollectionChanged;
        }

        [IgnoreDataMember]
        public bool Saved { get; private set; }

        public string Name
        {
            get { return m_Name; }
            set
            {
                if (m_Name != value)
                {
                    m_Name = value;
                    NameChanged(this, EventArgs.Empty);
                    Invalidate(this, EventArgs.Empty);
                }
            }
        }

        public ObservableCollection<Folder> Folders
        {
            get { return _folders; }
        }

        [IgnoreDataMember]
        public string FileName { get; private set; }

        public event EventHandler<EventArgs> DocumentChanged;
        public event EventHandler<EventArgs> DocumentSaved;
        public event EventHandler<EventArgs> NameChanged;

        public void Save(bool forceNewName)
        {
            if (forceNewName || FileName == "Not Saved")
            {
                SaveFileDialog Dialog = new SaveFileDialog();
                Dialog.Filter = "Sprite Utility Files (*.suf)|*.suf";
                ImageViewer.Paused = true;
                DialogResult Result = Dialog.ShowDialog();
                ImageViewer.Paused = false;
                if (Result == DialogResult.OK)
                {
                    FileName = Dialog.FileName;
                }
                else
                {
                    return;
                }
            }

            var json = JsonSerializer.Serialize(this);
            File.WriteAllText(FileName, json);
            
            FolderCollectionChanged(this, EventArgs.Empty);
            Saved = true;
            DocumentSaved(this, EventArgs.Empty);
        }

        public static void New()
        {
            var document = new Document();
            _instance = document;
        }

        public static void Open()
        {
            string fileName;

            var dialog = new OpenFileDialog();
            dialog.Filter = "Sprite Utility Files (*.suf)|*.suf";
            ImageViewer.Paused = true;
            DialogResult result = dialog.ShowDialog();
            ImageViewer.Paused = false;
            if (result == DialogResult.OK)
                fileName = dialog.FileName;
            else
                return;

            var json = File.ReadAllText(fileName);
            var deserialized = JsonSerializer.Deserialize<Document>(json);
            SetFrameParents(deserialized.Folders);


            var newDocument = new Document { FileName = fileName };
            newDocument.Saved = true;
            newDocument.m_Name = deserialized.Name;
            
            m_TriggerInvalidate = true;
            var folderCount = deserialized.Folders.Count;
            m_TriggerInvalidate = false;

            for (var i = 0; i < folderCount; i++)
            {
                var toCopy = deserialized.Folders[i];
                var newFolder = new Folder();
                newFolder.Name = toCopy.Name;

                foreach (var folder in toCopy.Folders)
                {
                    var childNewFolder = new Folder();
                    childNewFolder.Name = folder.Name;
                    foreach (var image in folder.Images)
                    {
                        childNewFolder.Add(image);
                    }
                    newFolder.Add(childNewFolder);
                }

                foreach (var image in toCopy.Images)
                {
                    newFolder.Add(image);
                }
                newDocument.Folders.Add(newFolder);
            }
            m_TriggerInvalidate = true;
            
            _instance = newDocument;
            _instance.Saved = true;
        }

        private static void SetFrameParents(IEnumerable<Folder> folders)
        {
            // Flatten folder hierarchy
            foreach (var folder in folders)
            {
                if (folder.Images != null)
                {
                    foreach (var image in folder.Images)
                    {
                        foreach (var frame in image.Frames)
                        {
                            foreach (var polygonGroup in frame.PolygonGroups)
                            {
                                polygonGroup.SetFrameParent(frame);
                                foreach (var polygon in polygonGroup.Polygons)
                                {
                                    polygon.SetPolygonGroupParent(polygonGroup);
                                    foreach (var point in polygon.Points)
                                    {
                                        point.SetPolygonParent(polygon);
                                    }
                                }
                            }
                        }
                    }
                }
                if (folder.Folders != null)
                {
                    SetFrameParents(folder.Folders); 
                }
            }
        }

        public void Export(string fileName)
        {
            var export = new DocumentExport(this);
            var json = JsonSerializer.Serialize(export);
            File.WriteAllText(fileName, json);
        }

        protected virtual void OnDocumentChanged(object sender, EventArgs e)
        {
        }

        protected virtual void OnDocumentSaved(object sender, EventArgs e)
        {
        }

        protected virtual void OnNameChanged(object sender, EventArgs e)
        {
        }

        private void FolderCollectionChanged(object sender, EventArgs e)
        {
            Invalidate(this, EventArgs.Empty);
        }

        public void Invalidate(object sender, EventArgs e)
        {
            if (m_TriggerInvalidate)
            {
                DocumentChanged(sender, e);
                Saved = false;
            }
        }
    }
}