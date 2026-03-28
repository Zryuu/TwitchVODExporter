using VODExporter.Twitch;

namespace VODExporter.GUI;

public class CommandManager(StreamerManager sm, Logger logger)
{
    
    public void HelpCommand()
    {
        const string message = "Available Commands:\n" +
                               "\n" +
                               "  add <username>                 - Adds a streamer.\n" +
                               "  remove <username>              - Removes a streamer.\n" +
                               "  addvod <username> <VOD ID>     - Adds a VOD to the streamer's export list.\n" +
                               "  removevod <username> <VOD ID>  - Removes a VOD from the streamer's export list.\n" +
                               "  bl <username> <VOD ID>         - Blacklists a VOD from a streamer's list.\n" +
                               "  rbl <username> <VOD ID>        - Removes a a VOD from a streamer's Blacklist.\n" +
                               "  restore <username> <VOD ID>    - Restores a blacklisted VOD.\n";

        logger.Log(message);
    }

    public void AddStreamerToList(string username)
    {
        _ = sm.AddStreamer(username);
    }

    public void RemoveStreamerFromList(string username)
    {
        _ = sm.RemoveStreamer(username);
    }

    public void AddVodToList(string username, string id)
    {
        if (username == "" || id == "")
        {
            logger.Log($"Failed to execute [blue]AddVod[/] command. Either the username or id given were empty. " +
                       $"username: {username}, id: {id}", ELogType.Error);
            return;
        }
        
        sm.AddVod(username, id);
        logger.Log($"Added vod to list.", ELogType.Ok);
    }

    public void RemoveVodFromList(string username, string id)
    {
        if (username == "" || id == "")
        {
            logger.Log($"Failed to execute [blue]RemoveVod[/] command. Either the username or id given were empty. " +
                       $"username: {username}, id: {id}", ELogType.Error);
            return;
        }
        
        sm.RemoveVod(username, id);
        logger.Log($"Removed vod from list.", ELogType.Ok);
    }
    
    public void BlackListVod(string username, string id)
    {
        if (username == "" || id == "")
        {
            logger.Log($"Failed to execute [blue]Blacklist[/] command. Either the username or id given were empty. " +
                       $"username: {username}, id: {id}", ELogType.Error);
            return;
        }
        
        if (sm.AddBlackListEntry(username, id))
            logger.Log($"Blacklisted vod from {username}'s exports.", ELogType.Ok);
    }
    
    public void RemoveBlacklistVod(string username, string id)
    {
        if (username == "" || id == "")
        {
            logger.Log($"Failed to execute [blue]RemoveBlacklist[/] command. Either the username or id given were empty. " +
                       $"username: {username}, id: {id}", ELogType.Error);
            return;
        }
        
        if (sm.RemoveBlackListEntry(username, id))
            logger.Log($"removed vod from {username}'s Blacklist.", ELogType.Ok);
    }
    
    public void RestoreVod(string username, string id)
    {
        if (username == "" || id == "")
        {
            logger.Log($"Failed to execute [blue]Restore[/] command. Either the username or id given were empty. " +
                       $"username: {username}, id: {id}", ELogType.Error);
            return;
        }

        if (!sm.RemoveBlackListEntry(username, id))
            return;
        if (sm.AddVod(username, id))
            logger.Log($"Restored vod from {username}'s Blacklist.", ELogType.Ok);
    }

    public void Active(string username)
    {
        if (username == "" || username == "")
        {
            logger.Log($"Failed to execute [blue]Active[/] command. The username given was empty. " +
                       $"username: {username}", ELogType.Error);
            return;
        }

        var streamer = sm.GetStreamer(username);

        if (streamer == null)
        {
            logger.Log($"Failed to execute [blue]Active[/] command. No matching streamer in list. " +
                       $"username: {username}", ELogType.Error);
            return;
        }

        if (sm.UpdateActive(username, !streamer.Active))
            logger.Log($"Updated Active status for {username} to {streamer.Active}", ELogType.Ok);
    }
}