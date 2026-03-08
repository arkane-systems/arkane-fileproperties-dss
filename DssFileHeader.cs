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

using ArkaneSystems.Arkane.FileProperties.Properties;
using System;
using System.Globalization;
using System.IO;
using System.Text;

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
    public DssFileHeader (string fileName)
    {
      ArgumentException.ThrowIfNullOrEmpty (fileName);

      var dssStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);

      try
      {
        this.InitializeFromStream (dssStream);
      }
      finally
      {
        dssStream.Dispose ();
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
    public DssFileHeader (FileStream dssStream)
    {
      ArgumentNullException.ThrowIfNull (dssStream);

      FileStream stream = dssStream;

      if (!stream.CanRead)
        throw new InvalidOperationException (Resources.StreamNotReadable);

      if (!stream.CanSeek && stream.Position != 0L)
        throw new InvalidOperationException (Resources.CantSeekAndNotAtStart);

      long position = stream.Position;

      if (stream.CanSeek)
        stream.Position = 0L;

      this.InitializeFromStream (stream);

      if (stream.CanSeek)
        stream.Position = position;
    }

    /// <summary>
    /// Parse the header of the DSS file contained in dssStream.
    /// </summary>
    /// <param name="dssStream">A FileStream containing the DSS file to parse.</param>
    /// <remarks>
    /// <para>InitializeFromStream requires dssStream to be located at the start of file when it
    /// is called, and reads 1024 bytes from the stream, leaving the stream positioned after the
    /// header.</para>
    /// <para>Saving and restoring the position of the stream is handled by the constructor around
    /// the call to InitializeFromStream.
    /// </para>
    /// </remarks>
    protected void InitializeFromStream (FileStream dssStream)
    {
      ArgumentNullException.ThrowIfNull (dssStream);

      FileStream stream = dssStream;

      if (stream.Position != 0L)
        throw new InvalidOperationException (Resources.NotAtStart);

      BinaryReader binaryReader = new BinaryReader(stream, Encoding.ASCII, true);

      byte[] bytes;

      try
      {
        bytes = binaryReader.ReadBytes (1024);
      }
      catch (IOException ex)
      {
        throw new InvalidDataException (Resources.NotValidFile, ex);
      }

      this.PathName = stream.Name;

      if (bytes.Length < 1024)
        throw new InvalidDataException (Resources.NotValidFile);

      if (Encoding.ASCII.GetString (bytes, 1, 3) != "dss")
        throw new InvalidDataException (Resources.NotValidFile);

      try
      {
        this.CreatedOn = MakeDateTime (Encoding.ASCII.GetString (bytes, 38, 12));
        this.CompletedOn = MakeDateTime (Encoding.ASCII.GetString (bytes, 50, 12));
        this.Length = MakeTimeSpan (Encoding.ASCII.GetString (bytes, 62, 6));
      }
      catch (Exception ex) when (ex is ArgumentOutOfRangeException || ex is FormatException || ex is OverflowException)
      {
        throw new InvalidDataException (Resources.NotValidFile, ex);
      }

      // Get comments (null-terminated string)
      string commentTemp = Encoding.ASCII.GetString(bytes, 798, 100);
      int terminatorIndex = commentTemp.IndexOf('\0');
      this.Comments = terminatorIndex >= 0 ? commentTemp.Substring (0, terminatorIndex) : commentTemp;
    }

    #region Header properties
    /// <summary>
    /// The path name of the DSS file.
    /// </summary>
    public string PathName { get; private set; } = string.Empty;

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
    public string Comments { get; private set; } = string.Empty;

    #endregion

    #region Helper functions

    private static DateTime MakeDateTime (string datetime)
    {
      int year = int.Parse(datetime.Substring(0, 2), CultureInfo.InvariantCulture) + 2000;
      if (year > CultureInfo.InvariantCulture.Calendar.TwoDigitYearMax)
        year -= 100;
      return new DateTime (year,
                          int.Parse (datetime.Substring (2, 2), CultureInfo.InvariantCulture),
                          int.Parse (datetime.Substring (4, 2), CultureInfo.InvariantCulture),
                          int.Parse (datetime.Substring (6, 2), CultureInfo.InvariantCulture),
                          int.Parse (datetime.Substring (8, 2), CultureInfo.InvariantCulture),
                          int.Parse (datetime.Substring (10, 2), CultureInfo.InvariantCulture));
    }

    private static TimeSpan MakeTimeSpan (string span) => new TimeSpan (int.Parse (span.Substring (0, 2), CultureInfo.InvariantCulture),
                            int.Parse (span.Substring (2, 2), CultureInfo.InvariantCulture),
                            int.Parse (span.Substring (4, 2), CultureInfo.InvariantCulture));

    #endregion
  }
}
