 using System;
using System.Collections.Generic;

namespace EasyLog
{
    // Interface
    public interface ILogProvider
    {
        /// Écrit une entrée de log 
        void Write(LogEntry entry);

        /// Lit toutes les entrées de log d'une journée + date du log si besoin
        List<LogEntry> ReadByDate(DateTime date);
    }
}
