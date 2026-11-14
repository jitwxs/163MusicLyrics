using MusicLyricApp.Core;
using MusicLyricApp.Models;

namespace MusicLyricAppTest.Models;

public class LyricTimestampTest
{
    [Fact]
    public void Test_Constructor_With_Milliseconds()
    {
        // Arrange
        long milliseconds = 123456;
        
        // Act
        var timestamp = new LyricTimestamp(milliseconds);
        
        // Assert
        Assert.Equal(milliseconds, timestamp.TimeOffset);
    }
    
    [Theory]
    [InlineData("[00:00.000]", 0)]
    [InlineData("[00:01.000]", 1000)]
    [InlineData("[01:00.000]", 60000)]
    [InlineData("[01:01.000]", 61000)]
    [InlineData("[00:00.00]", 0)]
    [InlineData("[00:00.0]", 0)]
    [InlineData("[00:00]", 0)]
    [InlineData("[01:01.50]", 61500)]
    [InlineData("[01:01.5]", 61500)]
    public void Test_Constructor_With_Valid_Timestamp_String(string input, long expectedMilliseconds)
    {
        // Act
        var timestamp = new LyricTimestamp(input);
        
        // Assert
        Assert.Equal(expectedMilliseconds, timestamp.TimeOffset);
    }
    
    [Theory]
    [InlineData("[00:00:000]", 0)]
    [InlineData("[00:01:500]", 1500)]
    [InlineData("[01:01:050]", 61050)]
    [InlineData("[01:01:50]", 61500)]
    [InlineData("[01:01:5]", 61500)]
    public void Test_Constructor_With_Valid_Timestamp_String_Colon_Separated(string input, long expectedMilliseconds)
    {
        // Act
        var timestamp = new LyricTimestamp(input);
        
        // Assert
        Assert.Equal(expectedMilliseconds, timestamp.TimeOffset);
    }
    
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("[00:00.000")]
    [InlineData("00:00.000]")]
    [InlineData("00:00.000")]
    [InlineData("[00:00:00:000]")]
    [InlineData("[abc:def.ghi]")]
    public void Test_Constructor_With_Invalid_Timestamp_String(string? input)
    {
        // Act
        var timestamp = new LyricTimestamp(input);
        
        // Assert
        Assert.Equal(0, timestamp.TimeOffset);
    }
    
    [Theory]
    [InlineData("[0]", 0)]
    [InlineData("[1]", 60000)]
    [InlineData("[10]", 600000)]
    public void Test_Constructor_With_Minute_Only_Format(string input, long expectedMilliseconds)
    {
        // Act
        var timestamp = new LyricTimestamp(input);
        
        // Assert
        Assert.Equal(expectedMilliseconds, timestamp.TimeOffset);
    }
    
    [Fact]
    public void Test_Add()
    {
        // Arrange
        var timestamp = new LyricTimestamp(10000); // 10 seconds
        long addMilliseconds = 5000; // 5 seconds
        
        // Act
        var result = timestamp.Add(addMilliseconds);
        
        // Assert
        Assert.Equal(15000, result.TimeOffset);
        // Ensure original timestamp is not modified
        Assert.Equal(10000, timestamp.TimeOffset);
    }
    
    [Theory]
    [InlineData(1000, 2000, -1)] // Less than
    [InlineData(2000, 2000, 0)]  // Equal
    [InlineData(3000, 2000, 1)]  // Greater than
    [InlineData(-1, 2000, -1)]   // -1 is special value for less than
    [InlineData(2000, -1, 1)]    // -1 is special value for greater than when comparing to it
    public void Test_CompareTo(long first, long second, int expected)
    {
        // Arrange
        var firstTimestamp = new LyricTimestamp(first);
        var secondTimestamp = new LyricTimestamp(second);
        
        // Act
        var result = firstTimestamp.CompareTo(secondTimestamp);
        
        // Assert
        Assert.Equal(expected, result);
    }
    
    [Fact]
    public void Test_CompareTo_With_Non_LyricTimestamp_Object()
    {
        // Arrange
        var timestamp = new LyricTimestamp(1000);
        var notATimestamp = "not a timestamp";
        
        // Act & Assert
        Assert.Throws<MusicLyricException>(() => timestamp.CompareTo(notATimestamp));
    }
    
    [Theory]
    [InlineData(0, "[00:00.000]")]
    [InlineData(1000, "[00:01.000]")]
    [InlineData(61000, "[01:01.000]")]
    [InlineData(61500, "[01:01.500]")]
    public void Test_PrintTimestamp_Default_Format(long milliseconds, string expected)
    {
        // Arrange
        var timestamp = new LyricTimestamp(milliseconds);
        
        // Act
        var result = timestamp.PrintTimestamp("[mm:ss.SSS]", MusicLyricApp.Models.DotTypeEnum.DOWN);
        
        // Assert
        Assert.Equal(expected, result);
    }
    
    [Theory]
    [InlineData(61567, "[01:01.56]", MusicLyricApp.Models.DotTypeEnum.DOWN)]
    [InlineData(61567, "[01:01.57]", MusicLyricApp.Models.DotTypeEnum.HALF_UP)]
    public void Test_PrintTimestamp_With_Rounding(long milliseconds, string expected, MusicLyricApp.Models.DotTypeEnum dotType)
    {
        // Arrange
        var timestamp = new LyricTimestamp(milliseconds);
        
        // Act
        var result = timestamp.PrintTimestamp("[mm:ss.SS]", dotType);
        
        // Assert
        Assert.Equal(expected, result);
    }
    
    [Theory]
    [InlineData(61567, "[01:01.5]", MusicLyricApp.Models.DotTypeEnum.DOWN)]
    [InlineData(61567, "[01:01.6]", MusicLyricApp.Models.DotTypeEnum.HALF_UP)]
    public void Test_PrintTimestamp_One_Digit_Ms(long milliseconds, string expected, MusicLyricApp.Models.DotTypeEnum dotType)
    {
        // Arrange
        var timestamp = new LyricTimestamp(milliseconds);
        
        // Act
        var result = timestamp.PrintTimestamp("[mm:ss.S]", dotType);
        
        // Assert
        Assert.Equal(expected, result);
    }
    
    [Theory]
    [InlineData(3661000, "[01:01:01.000]")] // 1 hour, 1 minute, 1 second
    [InlineData(7261500, "[02:01:01.500]")] // 2 hours, 1 minute, 1 second, 500 milliseconds
    public void Test_PrintTimestamp_With_Hour_Format(long milliseconds, string expected)
    {
        // Arrange
        var timestamp = new LyricTimestamp(milliseconds);
        
        // Act
        var result = timestamp.PrintTimestamp("[HH:mm:ss.SSS]", MusicLyricApp.Models.DotTypeEnum.DOWN);
        
        // Assert
        Assert.Equal(expected, result);
    }
    
    [Theory]
    [InlineData(61567, "[01:01]")] // No milliseconds part
    public void Test_PrintTimestamp_No_Milliseconds_Format(long milliseconds, string expected)
    {
        // Arrange
        var timestamp = new LyricTimestamp(milliseconds);
        
        // Act
        var result = timestamp.PrintTimestamp("[mm:ss]", MusicLyricApp.Models.DotTypeEnum.DOWN);
        
        // Assert
        Assert.Equal(expected, result);
    }
}