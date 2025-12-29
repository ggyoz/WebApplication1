-- TB_DEPT_INFO Table (부서정보 테이블) for Supabase (PostgreSQL)

CREATE TABLE public.TB_DEPT_INFO (
    DEPTCD      varchar(10)     NOT NULL,
    DEPTNAME    varchar(100)    NOT NULL,
    CORCD       varchar(10)     NOT NULL,
    SORTORDER     integer,
    NOTE        varchar(1000),
    USEYN       varchar(1)      DEFAULT 'Y' NOT NULL,
    CONSTRAINT PK_TB_DEPT_INFO PRIMARY KEY (DEPTCD)
);

-- Comments
COMMENT ON TABLE public.TB_DEPT_INFO IS '부서정보 테이블';

COMMENT ON COLUMN public.TB_DEPT_INFO.DEPTCD IS '부서코드';
COMMENT ON COLUMN public.TB_DEPT_INFO.DEPTNAME IS '부서명';
COMMENT ON COLUMN public.TB_DEPT_INFO.CORCD IS '법인코드';
COMMENT ON COLUMN public.TB_DEPT_INFO.SORTORDER IS '정렬순서';
COMMENT ON COLUMN public.TB_DEPT_INFO.NOTE IS '설명';
COMMENT ON COLUMN public.TB_DEPT_INFO.USEYN IS '사용여부';

-- Row Level Security (RLS)
ALTER TABLE public.TB_DEPT_INFO ENABLE ROW LEVEL SECURITY;

-- Policies
CREATE POLICY "Allow all for authenticated users"
ON public.TB_DEPT_INFO
FOR ALL
TO authenticated
USING (true);

CREATE POLICY "Enable read access for all users"
ON public.TB_DEPT_INFO
FOR SELECT
USING (true);