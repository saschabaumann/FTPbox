﻿/* License
 * This file is part of FTPbox - Copyright (C) 2012-2013 ftpbox.org
 * FTPbox is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published 
 * by the Free Software Foundation, either version 3 of the License, or (at your option) any later version. This program is distributed 
 * in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. 
 * See the GNU General Public License for more details. You should have received a copy of the GNU General Public License along with this program. 
 * If not, see <http://www.gnu.org/licenses/>.
 */
/* Common.cs
 * Commonly used functions are called from this class.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FTPboxLib
{
    public static partial class Common
    {
        #region Fields

        public static List<string> LocalFolders = new List<string>();       //Used to store all the local folders at all times
        public static List<string> LocalFiles = new List<string>();         //Used to store all the local files at all times

        public static Translations Languages = new Translations();          //Used to grab the translations from the translations.xml file

        #endregion

        #region Methods

        /// <summary>
        /// Encrypt the given password. Used to store passwords encrypted in the config file.
        /// </summary>
        public static string Encrypt(string password)
        {
            return Utilities.Encryption.AESEncryption.Encrypt(password, DecryptionPassword, DecryptionSalt);
        }

        /// <summary>
        /// Decrypt the given encrypted password.
        /// </summary>
        public static string Decrypt(string encrypted)
        {
            return Utilities.Encryption.AESEncryption.Decrypt(encrypted, DecryptionPassword, DecryptionSalt);
        }

        /// <summary>
        /// Checks the type of item in the specified path (the item must exist)
        /// </summary>
        /// <param name="p">The path to check</param>
        /// <returns>true if the specified path is a file, false if it's a folder</returns>
        public static bool PathIsFile(string p)
        {
            try
            {
                FileAttributes attr = File.GetAttributes(p);
                return (attr & FileAttributes.Directory) != FileAttributes.Directory;
            }
            catch
            {
                return !LocalFolders.Contains(p);
            }
        }

        /// <summary>
        /// Removes the part until the last slash from the provided string.
        /// </summary>
        /// <param name="path">the path to the item</param>
        /// <returns>name of the item</returns>
        public static string _name(string path)
        {
            if (path.Contains("/"))
                path = path.Substring(path.LastIndexOf("/"));
            if (path.Contains(@"\"))
                path = path.Substring(path.LastIndexOf(@"\"));
            if (path.StartsWith("/") || path.StartsWith(@"\"))
                path = path.Substring(1);
            return path;
        }

        /// <summary>
        /// Puts ~ftpb_ to the beginning of the filename in the given item's path
        /// </summary>
        /// <param name="cpath">the given item's common path</param>
        /// <returns>Temporary path to item</returns>
        public static string _tempName(string cpath)
        {
            if (!cpath.Contains("/") && !cpath.Contains(@"\"))
                return String.Format("~ftpb_{0}", cpath);

            string parent = cpath.Substring(0, cpath.LastIndexOf("/"));
            string temp_name = String.Format("~ftpb_{0}", _name(cpath));

            return String.Format("{0}/{1}", parent, temp_name);
        }

        /// <summary>
        /// Puts ~ftpb_ to the beginning of the filename in the given item's local path
        /// </summary>
        /// <param name="lpath">the given item's local path</param>
        /// <returns>Temporary local path to item</returns>
        public static string _tempLocal(string lpath)
        {
            lpath = lpath.ReplaceSlashes();
            string parent = lpath.Substring(0, lpath.LastIndexOf("/"));            

            return String.Format("{0}/~ftpb_{1}", parent, _name(lpath));
        }

        /// <summary>
        /// Checks a filename for chars that wont work with most servers
        /// </summary>
        public static bool IsAllowedFilename(string name)
        {
            return name.ToCharArray().All(IsAllowedChar) && IsNotReservedName(name);
        }

        /// <summary>
        /// Checks if a char is allowed, based on the allowed chars for filenames
        /// </summary>
        private static bool IsAllowedChar(char ch)
        {            
            return !Path.GetInvalidFileNameChars().Any(ch.Equals);
        }

        /// <summary>
        /// Checks if the given file name is one of the system-reserved names
        /// </summary>
        private static bool IsNotReservedName(string name)
        {
            return ! new[]
                {
                    "CON", "PRN", "AUX", "CLOCK$", "NUL", "COM0", "COM1", "COM2", "COM3", "COM4",
                    "COM5", "COM6", "COM7", "COM8", "COM9", "LPT0", "LPT1", "LPT2", "LPT3", "LPT4",
                    "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
                }.Any(name.Equals);
        }

        /// <summary>
        /// Checks if a file is still being used (hasn't been completely transfered to the folder)
        /// </summary>
        /// <param name="path">The file to check</param>
        /// <returns><c>True</c> if the file is being used, <c>False</c> if now</returns>
        public static bool FileIsUsed(string path)
        {
            FileStream stream = null;
            string name = null;

            try
            {
                var fi = new FileInfo(path);
                name = fi.Name;
                stream = fi.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
            catch
            {
                if (name != null)
                    Log.Write(l.Debug, "File {0} is locked: True", name);
                return true;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }
            if (!String.IsNullOrWhiteSpace(name))
                Log.Write(l.Debug, "File {0} is locked: False", name);
            return false;
        }

        /// <summary>
        /// get translated text related to the given message type.
        /// </summary>
        /// <param name="t">the type of message to translate</param>
        /// <returns></returns>
        public static string _(MessageType t)
        {
            switch (t)
            {
                default:
                    return null;
                case MessageType.ItemChanged:
                    return Languages.Get(Settings.General.Language + "/tray/changed", "{0} was changed.");
                case MessageType.ItemCreated:
                    return Languages.Get(Settings.General.Language + "/tray/created", "{0} was created.");
                case MessageType.ItemDeleted:
                    return Languages.Get(Settings.General.Language + "/tray/deleted", "{0} was deleted.");
                case MessageType.ItemRenamed:
                    return Languages.Get(Settings.General.Language + "/tray/renamed", "{0} was renamed to {1}.");
                case MessageType.ItemUpdated:
                    return Languages.Get(Settings.General.Language + "/tray/updated", "{0} was updated.");
                case MessageType.FilesOrFoldersUpdated:
                    return Languages.Get(Settings.General.Language + "/tray/FilesOrFoldersUpdated", "{0} {1} have been updated");
                case MessageType.FilesOrFoldersCreated:
                    return Languages.Get(Settings.General.Language + "/tray/FilesOrFoldersCreated", "{0} {1} have been created");
                case MessageType.FilesAndFoldersChanged:
                    return Languages.Get(Settings.General.Language + "/tray/FilesAndFoldersChanged", "{0} {1} and {2} {3} have been updated");
                case MessageType.ItemsDeleted:
                    return Languages.Get(Settings.General.Language + "/tray/ItemsDeleted", "{0} items have been deleted.");
                case MessageType.File:
                    return Languages.Get(Settings.General.Language + "/tray/file", "File");
                case MessageType.Files:
                    return Languages.Get(Settings.General.Language + "/tray/files", "Files");
                case MessageType.Folder:
                    return Languages.Get(Settings.General.Language + "/tray/folder", "Folder");
                case MessageType.Folders:
                    return Languages.Get(Settings.General.Language + "/tray/folders", "Folders");
                case MessageType.LinkCopied:
                    return Languages.Get(Settings.General.Language + "/tray/link_copied", "Link copied to clipboard");
                case MessageType.Connecting:
                    return Languages.Get(Settings.General.Language + "/tray/connecting", "FTPbox - Connecting...");
                case MessageType.Disconnected:
                    return Languages.Get(Settings.General.Language + "/tray/disconnected", "FTPbox - Disconnected");
                case MessageType.Reconnecting:
                    return Languages.Get(Settings.General.Language + "/tray/reconnecting", "FTPbox - Re-Connecting...");
                case MessageType.Listing:
                    return Languages.Get(Settings.General.Language + "/tray/listing", "FTPbox - Listing...");
                case MessageType.Uploading:
                    return Languages.Get(Settings.General.Language + "/tray/uploading", "Uploading {0}");
                case MessageType.Downloading:
                    return Languages.Get(Settings.General.Language + "/tray/downloading", "Downloading {0}");
                case MessageType.Syncing:
                    return Languages.Get(Settings.General.Language + "/tray/syncing", "FTPbox - Syncing");
                case MessageType.AllSynced:
                    return Languages.Get(Settings.General.Language + "/tray/synced", "FTPbox - All files synced");
                case MessageType.Offline:
                    return Languages.Get(Settings.General.Language + "/tray/offline", "FTPbox - Offline");
                case MessageType.Ready:
                    return Languages.Get(Settings.General.Language + "/tray/ready", "FTPbox - Ready");
                case MessageType.Nothing:
                    return "FTPbox";
                case MessageType.NotAvailable:
                    return Languages.Get(Settings.General.Language + "/tray/not_available", "Not Available");
            }
        }

        /// <summary>
        /// displays details of the thrown exception in the console
        /// </summary>
        /// <param name="error"></param>
        public static void LogError(Exception error)
        {
            Log.Write(l.Error, "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
            Log.Write(l.Error, "Message: {0}", error.Message);
            Log.Write(l.Error, "--");
            Log.Write(l.Error, "StackTrace: {0}", error.StackTrace);
            Log.Write(l.Error, "--");
            Log.Write(l.Error, "Source: {0} Type: {1}", error.Source, error.GetType().ToString());
            Log.Write(l.Error, "--");
            foreach (KeyValuePair<string, string> s in error.Data)
                Log.Write(l.Error, "key: {0} value: {1}", s.Key, s.Value);
            Log.Write(l.Error, "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
        }

        #endregion

        #region Properties

        public static string AppdataFolder
        {
            get
            {
                #if DEBUG   //on debug mode, build the portable version. (load settings from exe's folder 
                    return Environment.CurrentDirectory;
                #else       //on release, build the full version. (load settings from appdata)
                    return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"FTPbox");
                #endif
            }
        }

        public static string DebugLogPath
        {
            get { return Path.Combine(AppdataFolder, "Debug.html"); }
        }

        #endregion
    }
}