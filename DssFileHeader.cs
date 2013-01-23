#region header

// Arkane.FileProperties.Dss - DssFileHeader.cs
// 
// Alistair J. R. Young
// Arkane Systems
// 
// Copyright Arkane Systems 2012-2013.  All rights reserved.
// 
// Licensed and made available under MS-PL: http://opensource.org/licenses/ms-pl .
// 
// Created: 2013-01-23 4:45 PM

#endregion

using System;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.IO;
using System.Text;

using ArkaneSystems.Arkane.FileProperties.Properties;

namespace ArkaneSystems.Arkane.FileProperties
{
    /// <summary>
    /// File properties class to parse the header of Olympus DSS (Digital Speech Standard) files.
    /// </summary>
    public class DssFileHeader
    {
        /// <summary>
        /// Create a DssFileHeader containing the information found in the header of the DSS file specified
        /// by name.
        /// </summary>
        /// <param name="fileName">The path of the DSS file to parse.</param>
        public DssFileHeader(string fileName)
        {
            Contract.Requires<ArgumentNullException>(!String.IsNullOrEmpty(fileName), "fileName");

            var dssStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);

            try
            {
                this.InitializeFromStream(dssStream);
            }
            finally
            {
                dssStream.Dispose();
            }
        }

        /// <summary>
        /// Create a DssFileHeader containing the information found in the header of the DSS file accessed
        /// by the <see cref="FileStream"/> passed in.
        /// </summary>
        /// <param name="dssStream">A <see cref="FileStream"/> containing a DSS file.</param>
        /// <remarks>
        /// The <see cref="FileStream"/> must be readable. If the <see cref="FileStream"/> is not also seekable,
        /// the stream must be positioned at start of file, and will be positioned after the header (with 1024
        /// bytes read) after DssFileHeader has been initialized.  If it is seekable, the position in the file
        /// will be remembered and restored after the header is parsed.
        /// </remarks>
        public DssFileHeader(FileStream dssStream)
        {
            Contract.Requires<ArgumentNullException>(dssStream != null, "dssStream");
            Contract.Requires<InvalidOperationException>(dssStream.CanRead, Resources.StreamNotReadable);
            Contract.Requires<InvalidOperationException>(dssStream.CanSeek || dssStream.Position == 0L,
                                                         Resources.CantSeekAndNotAtStart);

            long position = dssStream.Position;

            if (dssStream.CanSeek)
                dssStream.Position = 0L;

            this.InitializeFromStream(dssStream);

            if (dssStream.CanSeek)
                dssStream.Position = position;
        }

        /// <summary>
        /// Parse the header of the DSS file contained in dssStream.
        /// </summary>
        /// <param name="dssStream">A FileStream containing the DSS file to parse.</param>
        /// <remarks>
        /// <para>Subclasses should note that InitializeFromStream requires dssStream to be located
        /// at the start of file when it is called, and reads 1024 bytes from the stream, leaving
        /// the stream positioned after the header.</para>
        /// <para>Saving and restoring the position of the stream is handled by the constructor around
        /// the call to InitializeFromStream; overriders of InitializeFromStream need not make any
        /// special efforts in this area.
        /// </para>
        /// </remarks>
        protected void InitializeFromStream(FileStream dssStream)
        {
            Contract.Requires<ArgumentNullException>(dssStream != null, "dssStream");
            Contract.Requires<InvalidOperationException>(dssStream.Position == 0L, Resources.NotAtStart);

            BinaryReader binaryReader = new BinaryReader((Stream)dssStream);

            byte[] bytes;
            
            try
            {
                bytes = binaryReader.ReadBytes(1024);
            }
            catch (IOException ex)
            {
                throw new InvalidDataException(Resources.NotValidFile, ex);
            }
            
            this.PathName = dssStream.Name;
            
            if (Encoding.ASCII.GetString(bytes, 1, 3) != "dss")
                throw new InvalidDataException(Resources.NotValidFile);
            
            this.CreatedOn = MakeDateTime(Encoding.ASCII.GetString(bytes, 38, 12));
            this.CompletedOn = MakeDateTime(Encoding.ASCII.GetString(bytes, 50, 12));
            this.Length = MakeTimeSpan(Encoding.ASCII.GetString(bytes, 62, 6));

            // Get comments (null-terminated string)
            string commentTemp = Encoding.ASCII.GetString(bytes, 798, 100);
            this.Comments = commentTemp.Substring(0, commentTemp.IndexOf("\0", StringComparison.Ordinal));
        }

        #region Header properties
        /// <summary>
        /// The path name of the DSS file.
        /// </summary>
        public string PathName { get; private set; }

        /// <summary>
        /// The creation date of the DSS file, per file header.
        /// </summary>
        public DateTime CreatedOn { get; private set; }

        /// <summary>
        /// The completion date of the DSS file, per file header.
        /// </summary>
        public DateTime CompletedOn { get; private set; }

        /// <summary>
        /// The length of the DSS file, per file header.
        /// </summary>
        public TimeSpan Length { get; private set; }

        /// <summary>
        /// Any embedded comments in the DSS file header.
        /// </summary>
        public string Comments { get; private set; }

        #endregion

        #region Helper functions

        private static DateTime MakeDateTime(string datetime)
        {
            int year = int.Parse(datetime.Substring(0, 2), CultureInfo.InvariantCulture) + 2000;
            if (year > CultureInfo.InvariantCulture.Calendar.TwoDigitYearMax)
                year -= 100;
            return new DateTime(year,
                                int.Parse(datetime.Substring(2, 2), CultureInfo.InvariantCulture),
                                int.Parse(datetime.Substring(4, 2), CultureInfo.InvariantCulture),
                                int.Parse(datetime.Substring(6, 2), CultureInfo.InvariantCulture),
                                int.Parse(datetime.Substring(8, 2), CultureInfo.InvariantCulture),
                                int.Parse(datetime.Substring(10, 2), CultureInfo.InvariantCulture));
        }

        private static TimeSpan MakeTimeSpan(string span)
        {
            return new TimeSpan(int.Parse(span.Substring(0, 2), CultureInfo.InvariantCulture),
                                int.Parse(span.Substring(2, 2), CultureInfo.InvariantCulture),
                                int.Parse(span.Substring(4, 2), CultureInfo.InvariantCulture));
        }

        #endregion
    }
}
