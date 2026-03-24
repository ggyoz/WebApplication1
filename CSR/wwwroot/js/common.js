
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
        placeholder: '날짜 선택',
        type: 'date',
        displayFormat: 'yyyy-MM-dd',
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