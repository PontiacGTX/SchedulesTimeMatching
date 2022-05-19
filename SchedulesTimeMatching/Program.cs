using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace SchedulesTimeMatching
{
    public class UserSchedule
    {
        public string User { get; set; }
        public Dictionary<string,int> Time { get; set; }
        public int Counter { get; set; }
    }
    public class UsersSchedule
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
        string Path { get; set; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<Dictionary<String,bool>> GetKeys(string? path=null)
        {
            if (Path is null && path is null)
                throw new NullReferenceException("Path value cannot be null");

            if (!string.IsNullOrEmpty(path))
                Path = path;

           await ReadDataAsync(Path!);
         
            return src.Select(x => 
            {
               return new { Key = x.Substring(0, x.IndexOf("=")), Value = true, }; 
            }).ToDictionary(x=>x.Key,x=>x.Value);

        }
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
            Path = path;

            await ReadDataAsync(path);

            if (src is null)
                throw new InvalidDataException();

            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        async Task<string[]> ReadDataAsync(string path)
        {
            return src = await File.ReadAllLinesAsync(path);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        async IAsyncEnumerable<UsersSchedule> DeserializeData()
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
                    UsersSchedule userSchedule = new UsersSchedule();
                    int beginIndex = input.IndexOf("=")+1;
                    string[] values = input.Substring(beginIndex,input.Length-beginIndex).Split(",");
                    userSchedule.User = user;
                    userSchedule.Time = values.ToList();//match.Groups.Values.Select(x => x.Value).ToList();

                    yield return userSchedule;
                }
            }
             yield return null;

        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<List<UsersSchedule>> DeserializeDataToListAsync()
        {
            List<UsersSchedule> results = new List<UsersSchedule>(); 
            await foreach (var data in DeserializeData())
            {
                if(data is not null && data.Time.Any(x=>x is not null))
                results.Add(data);
            }
            return await Task.FromResult(results);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<Dictionary<string, Dictionary<string, bool>>> GetSchedulingDictionary(string path = null)
        {
            if (path is null && src is null)
            {
                throw new NullReferenceException("neither path or source doesnt contain valid data");
            }

            if (src is null)
                await ReadDataAsync(Path);

            List<UsersSchedule> users = await DeserializeDataToListAsync();
            Dictionary<string, Dictionary<string,bool>> usersScheduleDic = new();
            foreach (var userData in users)
            {
                UserSchedule usersScheduleObj = null;
                foreach(var dataTime in userData.Time)
                {
                    if(usersScheduleDic.TryGetValue(dataTime, out Dictionary<string,bool> usersValues))
                    {
                        if(!usersValues.TryGetValue(userData.User,out bool exist))
                        {
                            usersValues.Add(userData.User, true);
                            usersScheduleDic[dataTime] = usersValues;
                        }
                    }
                    else
                    {
                        usersScheduleDic.Add(dataTime, new Dictionary<string, bool> { {userData.User, true } });

                    }

                }

            }
            return usersScheduleDic;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<IEnumerable<ScheduleUsers>> GetMatchingSchedule(string path = null)
        {
            if(path is null && src is null)
            {
                throw new NullReferenceException("neither path or source doesnt contain valid data");
            }

            if (src is null)
               await ReadDataAsync(path);

            List<UsersSchedule> users =await DeserializeDataToListAsync();
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
        public async Task<Dictionary<string, Dictionary<string, UserSchedule>>> GetUsersMatchingSchedule(Dictionary<string, Dictionary<string, bool>> resultUsersSchedule, Dictionary<String, bool> userMaps)
        {
            Dictionary<string, Dictionary<string, UserSchedule>> usersMatch = new();
            if (userMaps is null)
                throw new NullReferenceException($"User map cannot be null");

            if(resultUsersSchedule is null)
            {
                throw new NullReferenceException($"Users matching schedule must not be null");
            }
            foreach (var user in userMaps.Keys)
            {
                var cpyTargetUser = user;

                foreach (var dateData in resultUsersSchedule)
                {
                    if (dateData.Value.TryGetValue(cpyTargetUser, out bool exists))
                    {
                        foreach (var x in dateData.Value.Keys)
                        {
                            if (cpyTargetUser != x)
                            {
                                var temp = new UserSchedule { User = x, Time = new Dictionary<string, int> { { dateData.Key, 1 } }, Counter = 1 };
                                if (!usersMatch.TryAdd(cpyTargetUser, new Dictionary<string, UserSchedule> { { x, temp } }))
                                {
                                    if (usersMatch.TryGetValue(cpyTargetUser, out Dictionary<string, UserSchedule> userSchDic))
                                    {
                                        if (userSchDic.TryGetValue(x, out UserSchedule userSchedule))
                                        {
                                            if (!userSchedule.Time.TryGetValue(dateData.Key, out int timeCtr))
                                            {
                                                userSchedule.Counter++;
                                              
                                                userSchedule.Time.TryAdd(dateData.Key, 1);
                                            }
                                            userSchDic[x] = userSchedule;
                                        }
                                        else
                                        {
                                            userSchDic.TryAdd(x, temp);
                                        }
                                        continue;
                                    }
                                }
                            }

                        }
                    }
                }

            }
            return usersMatch;
        }
    }
    public static class Program
    {

        public static async Task Main(string[] args)
        {

           
            ScheduleMatching scheduleMatching = new ScheduleMatching();
            await scheduleMatching.FromFile("dat.txt");
            var results = await scheduleMatching.GetMatchingSchedule();
            Dictionary<string, Dictionary<string, bool>> re = await scheduleMatching.GetSchedulingDictionary();
            var arrUsers = await scheduleMatching.GetKeys();
            var usersMatch = await scheduleMatching.GetUsersMatchingSchedule(re,arrUsers);
            foreach (var usermatch in usersMatch)
            {
                Console.WriteLine($"{usermatch.Key} worked with {string.Join(",", usermatch.Value.Values.SelectMany(x => x.Time.Keys.Select(y => new { Time = y, Times = x.Time[y], x.User, TotalTimesWorked = x.Time.Count(), NL = "\n" })))}");
            }
            foreach (var schedule in results)
            {
                Console.WriteLine($"{schedule.Time} users:{string.Join(',', schedule.Users)}");
            }
           

        }
    }
}