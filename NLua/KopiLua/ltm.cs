namespace KopiLua
{
    using TValue = Lua.LuaTypeValue;

    public partial class Lua
    {
        /*
        * WARNING: if you change the order of this enumeration,
        * grep "ORDER TM"
        */

        public enum TMS
        {
            TM_INDEX,
            TM_NEWINDEX,
            TM_GC,
            TM_MODE,
            TM_EQ,  /* last tag method with `fast' access */
            TM_ADD,
            TM_SUB,
            TM_MUL,
            TM_DIV,
            TM_MOD,
            TM_POW,
            TM_UNM,
            TM_LEN,
            TM_LT,
            TM_LE,
            TM_CONCAT,
            TM_CALL,
            TM_N		/* number of elements in the enum */
        };

        public static TValue gfasttm(GlobalState g, Table et, TMS e)
        {
            return (et == null) ? null :
            ((et.flags & (1 << (int)e)) != 0) ? null :
            luaT_gettm(et, e, g.tmname[(int)e]);
        }

        public static TValue fasttm(LuaState l, Table et, TMS e)
        {
            return gfasttm(G(l), et, e);
        }

        public readonly static CharPtr[] luaT_typenames = {
		  "nil", "boolean", "userdata", "number",
		  "string", "table", "function", "userdata", "thread",
		  "proto", "upval"
		};

        private readonly static CharPtr[] luaT_eventname = {  /* ORDER TM */
			"__index", "__newindex",
			"__gc", "__mode", "__eq",
			"__add", "__sub", "__mul", "__div", "__mod",
			"__pow", "__unm", "__len", "__lt", "__le",
			"__concat", "__call"
		  };

        public static void luaT_init(LuaState L)
        {
            int i;
            for (i = 0; i < (int)TMS.TM_N; i++)
            {
                G(L).tmname[i] = luaS_new(L, luaT_eventname[i]);
                luaS_fix(G(L).tmname[i]);  /* never collect these names */
            }
        }

        /*
        ** function to be used with macro "fasttm": optimized for absence of
        ** tag methods
        */

        public static TValue luaT_gettm(Table events, TMS event_, TString ename)
        {
            /*const*/
            TValue tm = luaH_getstr(events, ename);
            LuaAssert(event_ <= TMS.TM_EQ);
            if (TTIsNil(tm))
            {  /* no tag method? */
                events.flags |= (byte)(1 << (int)event_);  /* cache this fact */
                return null;
            }
            else return tm;
        }

        public static TValue luaT_gettmbyobj(LuaState L, TValue o, TMS event_)
        {
            Table mt;
            switch (TType(o))
            {
                case LUA_TTABLE:
                    mt = HValue(o).metatable;
                    break;

                case LUA_TUSERDATA:
                    mt = UValue(o).metatable;
                    break;

                default:
                    mt = G(L).mt[TType(o)];
                    break;
            }

            return ((mt != null) ? luaH_getstr(mt, G(L).tmname[(int)event_]) : LuaONilObject);
        }
    }
}