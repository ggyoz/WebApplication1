-- TB_VENDOR_INFO 테이블 생성 (Supabase / PostgreSQL용)
-- ==========================================

CREATE TABLE public.tb_vendor_info (
    vendorid      serial                NOT NULL, -- 업체 ID
    vendor_code   varchar(20)           NOT NULL, -- 업체 코드
    vendor_name   varchar(50)           NOT NULL, -- 업체명
    vendor_type   varchar(100)          NOT NULL, -- 업체 구분
    biz_reg_no    varchar(20),                    -- 사업자등록번호
    biz_type      varchar(50),                    -- 업태
    biz_item      varchar(50),                    -- 종목
    ceo_name      varchar(50),                    -- 대표차명
    tel_no        varchar(20),                    -- 대표전화번호
    email         varchar(100),                   -- 대표이메일
    address       varchar(255),                   -- 회사주소
    manager_name  varchar(50),                    -- 담당자명
    manager_phone varchar(20),                    -- 담당자 연락처
    manager_email varchar(100),                   -- 담당자 이메일
    status        varchar(20),                    -- 거래상태
    remarks       text,                           -- 비고
    useyn         char(1)      DEFAULT 'Y'::bpchar NOT NULL, -- 사용여부
    reg_date      timestamptz  DEFAULT now() NOT NULL,      -- 등록일
    reg_userid    varchar(50)           NOT NULL,      -- 등록자ID
    update_date   timestamptz,                         -- 수정일
    update_userid varchar(50),                         -- 수정자ID
    CONSTRAINT tb_vendor_info_pkey PRIMARY KEY (vendorid)
);

-- 주석 설정
COMMENT ON TABLE public.tb_vendor_info IS '벤더정보';
COMMENT ON COLUMN public.tb_vendor_info.vendorid IS '업체 ID';
COMMENT ON COLUMN public.tb_vendor_info.vendor_code IS '업체 코드';
COMMENT ON COLUMN public.tb_vendor_info.vendor_name IS '업체명';
COMMENT ON COLUMN public.tb_vendor_info.vendor_type IS '업체 구분';
COMMENT ON COLUMN public.tb_vendor_info.biz_reg_no IS '사업자등록번호';
COMMENT ON COLUMN public.tb_vendor_info.biz_type IS '업태';
COMMENT ON COLUMN public.tb_vendor_info.biz_item IS '종목';
COMMENT ON COLUMN public.tb_vendor_info.ceo_name IS '대표차명';
COMMENT ON COLUMN public.tb_vendor_info.tel_no IS '대표전화번호';
COMMENT ON COLUMN public.tb_vendor_info.email IS '대표이메일';
COMMENT ON COLUMN public.tb_vendor_info.address IS '회사주소';
COMMENT ON COLUMN public.tb_vendor_info.manager_name IS '담당자명';
COMMENT ON COLUMN public.tb_vendor_info.manager_phone IS '담당자 연락처';
COMMENT ON COLUMN public.tb_vendor_info.manager_email IS '담당자 이메일';
COMMENT ON COLUMN public.tb_vendor_info.status IS '거래상태';
COMMENT ON COLUMN public.tb_vendor_info.remarks IS '비고';
COMMENT ON COLUMN public.tb_vendor_info.useyn IS '사용여부';
COMMENT ON COLUMN public.tb_vendor_info.reg_date IS '등록일';
COMMENT ON COLUMN public.tb_vendor_info.reg_userid IS '등록자ID';
COMMENT ON COLUMN public.tb_vendor_info.update_date IS '수정일';
COMMENT ON COLUMN public.tb_vendor_info.update_userid IS '수정자ID';
