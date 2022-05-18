using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace SchedulesTimeMatching
{
    public class UserSchedule
    {
        public string User { get; set; }
        public IList<string?> Time { get; set; }
    }
    public class ScheduleUsers
    {
        public List<string> Users { get; set; }
        public String Time { get; set; }
    }
    public class ScheduleMatching
    {
        string[] src { get; set; } = null;
        public async Task<bool> FromFile(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new NullReferenceException("File's path Is invalid");
            }
            if (!File.Exists(path))
            {
                return false;
            }

            await ReadDataAsync(path);

            if (src is null)
                throw new InvalidDataException();

            return true;
        }
        async Task<string[]> ReadDataAsync(string path)
        {
            return src = await File.ReadAllLinesAsync(path);
        }
        async IAsyncEnumerable<UserSchedule> DeserializeData()
        {
            var cultureEnUs = new CultureInfo("en-US");
            foreach (var input in src)
            {
                bool userFound = false;
                string user = null;
                try
                {
                     user = input.Substring(0, input.IndexOf("="));
                    userFound = true;
                }
                catch (Exception)
                {
                    userFound = false;
                }
                if(userFound)
                {
                    //Regex regex = new Regex("^([0-1]?[0-9]|2[0-3]):[0-5][0-9]$");
                    //var match = regex.Match(input);
                    UserSchedule userSchedule = new UserSchedule();
                    int beginIndex = input.IndexOf("=")+1;
                    string[] values = input.Substring(beginIndex,input.Length-beginIndex).Split(",");
                    userSchedule.User = user;
                    userSchedule.Time = values.ToList();//match.Groups.Values.Select(x => x.Value).ToList();

                    yield return userSchedule;
                }
            }
             yield return null;

        }

        public async Task<List<UserSchedule>> DeserializeDataToListAsync()
        {
            List<UserSchedule> results = new List<UserSchedule>(); 
            await foreach (var data in DeserializeData())
            {
                if(data is not null && data.Time.Any(x=>x is not null))
                results.Add(data);
            }
            return await Task.FromResult(results);
        }
        public async Task<IEnumerable<ScheduleUsers>> GetMatchingSchedule(string path = null)
        {
            if(path is null && src is null)
            {
                throw new NullReferenceException("neither path or source doesnt contain valid data");
            }

            if (src is null)
               await ReadDataAsync(path);

            List<UserSchedule> users =await DeserializeDataToListAsync();
            Dictionary<string?, Dictionary<string,bool>> usersDate = new();


            foreach(var user in users)
            {
                if(user is not null)
                foreach(var time in user.Time)
                {
                    if (time is not null)
                    {
                        if (usersDate.TryGetValue(time, out Dictionary<string, bool> usersStr))
                        {
                            if (!usersStr.TryGetValue(user.User, out bool found))
                            {
                                usersStr.Add(user.User, true);
                                usersDate[time] = usersStr;
                            }
                        }
                        else
                        {
                            var dic = new Dictionary<string, bool> { { user.User, true } };
                            usersDate.Add(time, dic);
                        }
                    }
                }
            }



            return usersDate.Select(x => new {schedule = x.Key, users = x.Value })
                .Where(x => x.users.Count() > 1).Select(x=>new ScheduleUsers { Time = x.schedule, Users = x.users.Keys.Select(x=>x).ToList() });
        }
    }
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            ScheduleMatching scheduleMatching = new ScheduleMatching();
            await scheduleMatching.FromFile("dat.txt");
            foreach(var schedule in await scheduleMatching.GetMatchingSchedule())
            {
                Console.WriteLine($"{schedule.Time} users:{string.Join(',', schedule.Users)}");
            }
        }
    }
}