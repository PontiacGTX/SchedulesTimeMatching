using System;
using System.Globalization;
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
        async Task<string[]> ReadDataAsync(string path)
        {
            return src = await File.ReadAllLinesAsync(path);
        }
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
            Dictionary<string, Dictionary<string, UserSchedule>> usersMatch = new();
            foreach(var user in arrUsers.Keys)
            {
                var cpyTargetUser = user;
                foreach(var key in arrUsers.Keys)
                {
                    foreach(var dateData in re)
                    {
                        if(dateData.Value.TryGetValue(cpyTargetUser, out bool exists))
                        {
                            foreach(var x in dateData.Value.Keys)
                            {
                                if (cpyTargetUser != x)
                                {
                                    var temp = new UserSchedule { User = x, Time = new Dictionary<string, int> { {dateData.Key, 1 } }, Counter = 1 };
                                    if (!usersMatch.TryAdd(cpyTargetUser,new Dictionary<string, UserSchedule> { { x , temp } }))
                                    {
                                        if (usersMatch.TryGetValue(cpyTargetUser, out Dictionary<string, UserSchedule> userSchDic))
                                        {
                                            if(userSchDic.TryGetValue(x, out UserSchedule userSchedule))
                                            {
                                                if(!userSchedule.Time.TryGetValue(dateData.Key,out int timeCtr))
                                                {
                                                    userSchedule.Counter++;
                                                //    timeCtr++;
                                                //    userSchedule.Time[dateData.Key] = timeCtr; 
                                                //}
                                                //else
                                                //{
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
            }
            foreach(var usermatch in usersMatch)
            {
                Console.WriteLine($"{usermatch.Key} worked with {string.Join(",", usermatch.Value.Values.Select(x => new { x.Time, x.User }).SelectMany(x => x.Time.Keys.Select(y => new { Time = y, Times = x.Time[y], x.User })))}");
            }
            foreach (var schedule in results)
            {
                Console.WriteLine($"{schedule.Time} users:{string.Join(',', schedule.Users)}");
            }
            string currentVal = "";
            Dictionary<UserSchedule, List<string>> usersMatchTime = new Dictionary<UserSchedule, List<string>>();
            foreach (var user in arrUsers.Keys)
            {
                currentVal = user;
                foreach (var schedule in results)
                {
                    UserSchedule userSchedule = new UserSchedule { Time = new Dictionary<string, int> { { schedule.Time, 1 } }, User = currentVal };
                    if(schedule.Users.Contains(user))
                    {
                        foreach(var innerUser in schedule.Users)
                        {
                            if(innerUser!=currentVal)
                            {
                                if(usersMatchTime.TryGetValue(userSchedule,out List<string> list))
                                {
                                    list.Add(innerUser);
                                    usersMatchTime[userSchedule] = list;
                                }
                                else
                                {
                                    usersMatchTime.Add(userSchedule, new List<string> { innerUser });
                                }
                            }

                        }
                    }
                }
            }
            string userTarget = "";
            Dictionary<Tuple<UserSchedule, string>,int> matches = new Dictionary<Tuple<UserSchedule, string>,int>();
            foreach(var k in usersMatchTime.Keys)
            {
                userTarget = k.User;
                foreach(var user in arrUsers.Keys)
                {
                    if(user !=userTarget)
                    {
                        foreach(var kv in usersMatchTime)
                        {
                            if(kv.Key.User== userTarget)
                            {
                                if (kv.Value.Any(x => x == user))
                                {
                                    var tuple = new Tuple<UserSchedule, string>(k, user);
                                    if (matches.TryGetValue(tuple, out int val))
                                    {
                                        val++;
                                        matches[tuple] = val;
                                    }
                                    else
                                    {
                                        matches.Add(tuple, 1);
                                    }

                                }
                            }
                        }
                    }
                }

            }
            var res = matches.GroupBy(x => x.Key.Item1.User);
           var grouping = usersMatchTime.GroupBy(x=>x.Key.User).Select(grp=>new { 
               Time = grp.FirstOrDefault().Key.Time, User = grp.FirstOrDefault().Key.User, UserSameDate = grp.FirstOrDefault().Value });

        }
    }
}