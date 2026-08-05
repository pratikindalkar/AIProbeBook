//document.querySelectorAll(".btn-save").forEach(btn => {
//    btn.addEventListener("click", function (e) {
//        e.preventDefault();

//        let row = this.closest("tr");

//        let data = {
//            Section: row.cells[0].innerText.trim(),
//            Type: row.cells[1].innerText.trim(),
//            Add: row.querySelector(".add").checked,
//            Edit: row.querySelector(".edit").checked,
//            Update: row.querySelector(".update").checked,
//            Delete: row.querySelector(".delete").checked
//        };

//        fetch('/AccessControl/User_Access', {
//            method: 'POST',
//            headers: {
//                'Content-Type': 'application/json'
//            },
//            body: JSON.stringify(data)
//        })
//            .then(res => res.json())
//            .then(res => {
//                if (res.success) {
//                    alert("Saved!");
//                } else {
//                    alert(res.msg);
//                }
//            });
//    });
//});

var successMsg = @Html.Raw(System.Text.Json.JsonSerializer.Serialize(TempData["SuccessMsg"]));
var errorMsg = @Html.Raw(System.Text.Json.JsonSerializer.Serialize(TempData["ErrorMsg"]));

if (successMsg) {
    Swal.fire({
        icon: 'success',
        title: successMsg,
        showConfirmButton: false,
        timer: 3000, // ⏱ 3 seconds
        timerProgressBar: true
    });
}

if (errorMsg) {
    Swal.fire({
        icon: 'error',
        title: errorMsg,
        showConfirmButton: false,
        timer: 3000,
        timerProgressBar: true
    });
}