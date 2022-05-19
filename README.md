SchedulesTimeMatching - Technical test

1. must run with .NET6
2. csv data must be comma separated without spaces
4. input should be written within a txt file and the file must be read from .FromFile(); method suing data from GetSchedulingDictionary() and GetKeys()

Implementation Details
FromFile method will read a file into a string array if it encounters a problem reading the file it will throw an exception
GetMatchingSchedule method will check whether the source in a string array is null aswell the path is null if it is true will throw an exception because the FromFile method wasn't called then will take the data transformed in a List<UsersSchedule> to a Dictionary<string,Dictionary<string,bool>> which key contains a string with the shift from each user and a Dicitonary as a Value which contains the user as string and a boolean
then based on the header from the .txt file containing the users will iterate through each user in the Dictionary<string,bool> and on eah key chck if the value on the GetMatchingSchedule method contains said user, and if so then add each of the Dictionary values which and the times where said user had worked with this same person for this shift
