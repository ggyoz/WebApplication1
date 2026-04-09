
function formatPhoneNumber(phoneNumber) {
  // 숫자 이외의 문자 제거 (Lodash의 _.replace 대신 순수 JS replace 사용)
  let formatted = '';
  formatted = phoneNumber.replace(/[^0-9]/g, '').replace(/^(\d{3})(\d{3,4})(\d{4})$/, `$1-$2-$3`);

  return formatted;
}

function NumberOnly(text){
  let formatted = '';
  formatted = text.replace(/[^0-9]/g, '');
  
  return formatted;
}

function enforceMinMaxValue(inputElement) {

    const minValue = Number(inputElement.min);
    const maxValue = Number(inputElement.max);            
    const currentValue = Number(inputElement.value);
    
    if (currentValue > maxValue) {
        inputElement.value = maxValue;
    }
    if (currentValue < minValue) {
        inputElement.value = minValue;
    }
}

/**
 * DevExtreme HTML 에디터를 초기화하고, 숨겨진 입력 필드 및 미리보기와 연결합니다.
 * @param {string} editorSelector - HTML 에디터로 사용할 div의 CSS 선택자 (예: '.html-editor')
 * @param {string} inputSelector - 에디터의 값을 저장할 숨겨진 input의 CSS 선택자 (예: 'input[name="CONTENTS_HTML"]')
 * @param {string} previewSelector - 실시간 미리보기를 표시할 요소의 CSS 선택자 (예: '.value-content')
 */
function initializeHtmlEditor(editorSelector, inputSelector, previewSelector) {
    // 1. 초기값 설정: 숨겨진 input에서 값을 가져와 에디터의 초기 값으로 사용합니다.
    const initialValue = $(inputSelector).val();

    // 2. dxHtmlEditor 인스턴스 생성
    const editorInstance = $(editorSelector).dxHtmlEditor({
        height: 500,
        value: initialValue,
        toolbar: {
            items: [
                'undo', 'redo', 'separator',
                'bold', 'separator',
                {
                    name: 'size',
                    acceptedValues: ['8pt', '10pt', '12pt', '14pt', '18pt', '24pt', '36pt'],
                }, 'separator',
                'alignLeft', 'alignCenter', 'alignRight', 'alignJustify', 'separator',
                'color', 'background', 'separator',
                // {
                //     name: 'header',
                //     acceptedValues: [false, 1, 2, 3, 4, 5],
                //     options: { inputAttr: { 'aria-label': 'Header' } },
                // },                
                'orderedList', 'bulletList', 'separator',
                'link', 'image', // 링크 및 이미지 추가 기능
            ],
            
        },

        // *** 이미지 업로드 설정 ***
        imageUpload: {
            fileUploadMode: 'server',
            uploadUrl: "/api/image/upload",
            fileTypes: [".jpg", ".jpeg", ".png", ".gif", ".webp"],
            uploadMethod: "POST",
            fileUploaderOptions: {
                onUploaded: function(e) {
                    // Assuming your server returns a JSON object like { "url": "path/to/image.png" }
                    const imageUrl = JSON.parse(e.request.response).url;
                    if (imageUrl != "") {
                        // 값 넣는 방법 ( 값 추가가 아님 )
                        // editorInstance.option("value", "<img width='100%' src='" + imageUrl + "'></img>");
                        // 이미지 삽입 방법
                        editorInstance.insertEmbed(editorInstance.getLength(), // Insert at the end
                            "extendedImage", imageUrl
                        );
                    }
                }
            }            
        },        
        onValueChanged(e) {

            // var maxLength = 4000; // 최대 글자수 설정
            // if (e.value.length > maxLength) {
            //     alert("최대 " + maxLength + "자까지만 입력할 수 있습니다.");    
            //     //e.value = e.value.substr(0, 3999);
            //     return false;//e.component.html(e.previousValue);
            // }

            $(inputSelector).val(e.value);
            if (previewSelector) {
                $(previewSelector).html(e.value);
            }
        },
        mediaResizing: {
            enabled: true
        }
    }).dxHtmlEditor('instance');

    // 4. 초기 미리보기 내용을 설정합니다.
    if (previewSelector) {
        $(previewSelector).html(initialValue);
    }
}

const labelMode = 'static';

const formatDate = (date) => {
    if (!date) return '';
    const d = new Date(date);
    if (isNaN(d.getTime())) return '';
    const year = d.getFullYear();
    const month = ('0' + (d.getMonth() + 1)).slice(-2);
    const day = ('0' + d.getDate()).slice(-2);
    return `${year}-${month}-${day}`;
};

function initDateBox(selector, hiddenSelector, value){
    const dateBox = $(selector).dxDateBox({        
        placeholder: 'Select Date',
        displayFormat: 'yyyy-MM-dd',
        showClearButton: true,
        useMaskBehavior: true,
        type: 'date',
        inputAttr: { 'aria-label': hiddenSelector.slice(1) },
        labelMode,
        value: value ? new Date(value) : null,
        onValueChanged: function (e) {
            const dateValue = e.value ? formatDate(e.value) : '';
            $(hiddenSelector).val(dateValue);
        },
        onInput: function(){
            $(hiddenSelector).val("");
        }
    }).dxDateBox('instance');

    // Set initial hidden input value
    if (value) {
        $(hiddenSelector).val(formatDate(new Date(value)));
    }
};


function goToReqList(status = "", filterType, mydata ) {
    let url = '/Req/Index';
    let params = [];

    if(status != ""){
        params.push("terminateYn=N");
    }        

    if (status) params.push('ProcStatusCd=' + status);

    if (filterType === 'my') params.push('ResUserName=' + userName );

    if (filterType === 'myReq') params.push('ReqUserName=' + userName );    

    if (filterType === 'delay') {
        params.push("terminateYn=N");
        params.push('ExpectDate=' + today );
    }

    if(filterType === 'thisweek') params.push('ReqDate=' + getReferenceDate());

    if (mydata === true) params.push('ReqUserName=' + userName );

    if (params.length > 0) {
        url += '?' + params.join('&');
    }

    location.href = url;
};

// 기준일 (가장 최근의 금요일)을 계산하여 표시
function getReferenceDate() {
    const today = new Date();
    let dayOfWeek = today.getDay(); // 0 = Sunday, 1 = Monday, ..., 5 = Friday, 6 = Saturday

    // 금요일(5)을 찾기 위해 날짜 조정
    // 만약 오늘이 일요일(0)부터 금요일(5)까지라면, 그 주의 금요일로
    // 만약 오늘이 토요일(6)이라면, 현재 날짜에서 1일 전 (오늘) 금요일로
    let daysToSubtract = 0;
    if (dayOfWeek < 5) { // 일(0), 월(1), 화(2), 수(3), 목(4)
        daysToSubtract = dayOfWeek + 2; // 예를 들어 월요일(1)이면 1+2=3일 빼서 지난 금요일로
    } else if (dayOfWeek === 6) { // 토요일(6)
        daysToSubtract = 1; // 1일 빼서 금요일로
    } else { // 금요일(5)
        daysToSubtract = 0;
    }
    
    const referenceDate = new Date(today);
    referenceDate.setDate(today.getDate() - daysToSubtract);

    const year = referenceDate.getFullYear();
    const month = (referenceDate.getMonth() + 1).toString().padStart(2, '0');
    const day = referenceDate.getDate().toString().padStart(2, '0');

    return `${year}-${month}-${day}`;    
}