-- TB_GROUP_MENU Table (GROUP별 MENU 매핑 테이블) for Supabase (PostgreSQL)

CREATE TABLE public.TB_GROUP_MENU (
    GROUPID     varchar(10)     NOT NULL,
    MENUID      varchar(40)     NOT NULL,
    NOTE        varchar(2000),
    CONSTRAINT PK_TB_GROUP_MENU PRIMARY KEY (GROUPID, MENUID)
);

-- Comments
COMMENT ON TABLE public.TB_GROUP_MENU IS 'GROUP별 MENU 매핑 테이블';

COMMENT ON COLUMN public.TB_GROUP_MENU.GROUPID IS '그룹 ID';
COMMENT ON COLUMN public.TB_GROUP_MENU.MENUID IS '메뉴 ID';
COMMENT ON COLUMN public.TB_GROUP_MENU.NOTE IS '설명';

-- Row Level Security (RLS)
ALTER TABLE public.TB_GROUP_MENU ENABLE ROW LEVEL SECURITY;

-- Policies
CREATE POLICY "Allow all for authenticated users"
ON public.TB_GROUP_MENU
FOR ALL
TO authenticated
USING (true);

CREATE POLICY "Enable read access for all users"
ON public.TB_GROUP_MENU
FOR SELECT
USING (true);