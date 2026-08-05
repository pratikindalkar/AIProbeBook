const openBtn = document.getElementById('ViewButton');
const openDeleteBtn = document.getElementById('DeleteButton');
const closeBtn = document.getElementById('closePopup');
const closeDeleteBtn = document.getElementById('closePopup');
const popup = document.getElementById('popup');
const overlay = document.getElementById('overlay');

openBtn.addEventListener('click', () => {
	popup.style.display = 'block';
	overlay.style.display = 'block';
});

closeBtn.addEventListener('click', () => {
	popup.style.display = 'none';
	overlay.style.display = 'none';
});

