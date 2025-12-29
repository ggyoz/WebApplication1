-- TB_USER_GROUP Table (USER와 GROUP 매핑 테이블) for Supabase (PostgreSQL)

CREATE TABLE public.TB_USER_GROUP (
    USERID      varchar(10)     NOT NULL,
    GROUPID     varchar(10)     NOT NULL,
    CONSTRAINT PK_TB_USER_GROUP PRIMARY KEY (USERID, GROUPID)
);

-- Comments
COMMENT ON TABLE public.TB_USER_GROUP IS 'USER와 GROUP 매핑 테이블';

COMMENT ON COLUMN public.TB_USER_GROUP.USERID IS '사용자 ID';
COMMENT ON COLUMN public.TB_USER_GROUP.GROUPID IS '그룹 ID';

-- Row Level Security (RLS)
ALTER TABLE public.TB_USER_GROUP ENABLE ROW LEVEL SECURITY;

-- Policies
CREATE POLICY "Allow all for authenticated users"
ON public.TB_USER_GROUP
FOR ALL
TO authenticated
USING (true);

CREATE POLICY "Enable read access for all users"
ON public.TB_USER_GROUP
FOR SELECT
USING (true);