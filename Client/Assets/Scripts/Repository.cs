using System;
using System.Collections.Generic;

public class Repository
{
    public static Repository I => repository ??= new Repository();
    private static Repository repository;

    public PersonalFullUserInfo PersonalFullInfo;
    public MinUserInfo[] YesterdayChampions;
    public MinUserInfo[] TopFriends;
}