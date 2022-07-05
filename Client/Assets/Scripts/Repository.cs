using System;
using System.Collections.Generic;

public class Repository : Singleton<Repository>
{
    public PersonalFullUserInfo PersonalFullInfo;
    public MinUserInfo[] YesterdayChampions;
    public MinUserInfo[] TopFriends;

    public Repository(PersonalFullUserInfo personalFullInfo, MinUserInfo[] yesterdayChampions,
        MinUserInfo[] topFriends) : base()
    {
        PersonalFullInfo = personalFullInfo;
        YesterdayChampions = yesterdayChampions;
        TopFriends = topFriends;
    }
}