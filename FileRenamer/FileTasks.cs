﻿using Microsoft.Extensions.Configuration;
using System.Data.SqlClient;

namespace FileRenamer
{
    public static class FileTasks
    {
        public static void Rename()
        {
            var config = GetConfig();
            var path = config.GetValue<string>("Path") ?? string.Empty;
            Rename(path);
        }

        /// <summary>
        /// Rename all subfolders and files in the given path
        /// </summary>
        /// <param name="path"></param>
        private static void Rename(string path)
        {
            string[] filesAndFolders = Directory.GetFileSystemEntries(path);
            foreach (string item in filesAndFolders)
            {
                if (File.Exists(item))
                {
                    ProcessFiles(path, item);
                }
                else if (Directory.Exists(item))
                {
                    ProcessDir(path, item);
                }
                else
                {
                    // invalid path
                    Console.WriteLine("Invalid path: " + item);// save to a log file
                }
            }
        }

        private static IConfiguration GetConfig()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("config.json", optional: false);

            return builder.Build();
        }

        private static void ProcessDir(string path, string item, int i = 0)
        {
            // is Folder
            string folderName = Path.GetFileName(item);
            string newFolderName;
            if (i > 0)
            {
                newFolderName = GetNameFromDB(folderName);
                newFolderName = newFolderName.Replace("/", string.Empty);
                newFolderName = CheckLength(newFolderName, 30);
                newFolderName = $"{newFolderName}~{i}";
            }
            else
            {
                newFolderName = GetNameFromDB(folderName);
                newFolderName = newFolderName.Replace("/", string.Empty);
                newFolderName = CheckLength(newFolderName, 30);
            }
            
            if (folderName != newFolderName)
            {
                string newFilePath = Path.Combine(path, newFolderName);
                if (Directory.Exists(newFilePath))
                {
                    ProcessDir(path, item, i + 1);
                }
                else
                {
                    Directory.Move(item, newFilePath);
                    Console.WriteLine("Directory renamed: " + item + " to " + newFolderName);
                    //Check if folder contains files/folders
                    Rename(newFilePath);
                }
            }
        }

        private static void ProcessFiles(string path, string item, int i = 0)
        {
            // is file
            string fileName = Path.GetFileNameWithoutExtension(item);
            var ext = Path.GetExtension(item);
            string newFileName;
            if (i > 0)
            {
                newFileName = $"{CheckLength(GetNameFromDB(fileName), 30)}~{i}{ext}";
            }
            else
            {
                newFileName = $"{CheckLength(GetNameFromDB(fileName), 30)}{ext}";
            }
            if (fileName != newFileName)
            {
                string newFilePath = Path.Combine(path, newFileName);
                if (File.Exists(newFilePath))
                {
                    ProcessFiles(path, item, i + 1);
                }
                else
                {
                    File.Move(item, newFilePath);
                    Console.WriteLine("File renamed: " + item + " to " + newFileName);
                }
            }
        }

        private static string CheckLength(string newFileName, int maxlength)
        {
            var length = newFileName.Length;
            if (length > maxlength)
            {
                newFileName = newFileName.Substring(0, maxlength);
            }
            return newFileName;
        }

        private static string GetNameFromDB(string folderName)
        {
            var config = GetConfig();
            var connString = config.GetValue<string>("ConnectionString") ?? string.Empty;
            if (int.TryParse(folderName, out int nodeId))
            {
                using var conn = new SqlConnection(connString);
                using var command = new SqlCommand("SELECT Name FROM dbo.Container WHERE NodeId = @NodeId", conn);
                command.Parameters.AddWithValue("@NodeId", nodeId);
                conn.Open();
                var result = command.ExecuteScalar();

                return result?.ToString() ?? folderName;
            }
            return folderName;
        }
    }
}
