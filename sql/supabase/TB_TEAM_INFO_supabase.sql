-- TB_TEAM_INFO Table (팀 정보 테이블) for Supabase (PostgreSQL)

CREATE TABLE public.TB_TEAM_INFO (
    TEAMCD      varchar(10)     NOT NULL,
    TEAMNAME    varchar(100)    NOT NULL,
    DEPTCD      varchar(10)     NOT NULL,
    SORTORDER     integer,
    NOTE        varchar(1000),
    USEYN       varchar(1)      DEFAULT 'Y' NOT NULL,
    CONSTRAINT PK_TB_TEAM_INFO PRIMARY KEY (TEAMCD)
);

-- Comments
COMMENT ON TABLE public.TB_TEAM_INFO IS '팀 정보 테이블';

COMMENT ON COLUMN public.TB_TEAM_INFO.TEAMCD IS '팀코드';
COMMENT ON COLUMN public.TB_TEAM_INFO.TEAMNAME IS '팀명';
COMMENT ON COLUMN public.TB_TEAM_INFO.DEPTCD IS '부서코드';
COMMENT ON COLUMN public.TB_TEAM_INFO.SORTORDER IS '정렬순서';
COMMENT ON COLUMN public.TB_TEAM_INFO.NOTE IS '설명';
COMMENT ON COLUMN public.TB_TEAM_INFO.USEYN IS '사용여부';

-- Row Level Security (RLS)
ALTER TABLE public.TB_TEAM_INFO ENABLE ROW LEVEL SECURITY;

-- Policies
CREATE POLICY "Allow all for authenticated users"
ON public.TB_TEAM_INFO
FOR ALL
TO authenticated
USING (true);

CREATE POLICY "Enable read access for all users"
ON public.TB_TEAM_INFO
FOR SELECT
USING (true);