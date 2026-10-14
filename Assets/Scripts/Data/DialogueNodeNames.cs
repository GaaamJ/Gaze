namespace ProjectT.Data
{
    public static class DialogueNodeNames
    {
        public static string Obj(string objectId) => $"Obj_{objectId}";
        public static string ObjRecheck(string objectId) => $"Obj_{objectId}_Recheck";
        public static string Death(string deathId) => $"Death_{deathId}";
        public static string DeathRefuse(string deathId) => $"Death_{deathId}_Refuse";
        public static string DeathRetrigger(string deathId) => $"Death_{deathId}_Retrigger";
        public static string DeathSign(string deathId) => $"Death_{deathId}_Sign";
        public static string DeathSign(string deathId, string resultId) => $"Death_{deathId}_Sign_{resultId}";
    }
}
