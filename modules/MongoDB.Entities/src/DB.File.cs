﻿using MongoDB.Bson;

using System;

namespace MongoDB.Entities
{
    public static partial class DB
    {
        /// <summary>
        /// Returns a DataStreamer object to enable downloading file data directly by supplying the Id of the file entity
        /// </summary>
        /// <typeparam name="T">The file entity type</typeparam>
        /// <param name="id">The Id of the file entity</param>
        public static DataStreamer File<T>(string id) where T : FileEntity, new()
        {
            return new DataStreamer(new T() { Id = id, UploadSuccessful = true });
        }
    }
}
