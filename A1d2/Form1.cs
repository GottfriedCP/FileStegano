using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace A1d2
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private static byte[] coverFileBytes, messageFileBytes, stegoFileBytes, generatedIdBlock;
        private static string coverFileType, messageFileType;
        private static bool coverFileExists, messageFileExists;

        private void Form1_Load(object sender, EventArgs e)
        {
            // Disable 'Hide' button on 'Hide' tab
            btnEncrypt.Enabled = false;
            // Disable 'Recover' button on 'Recover' tab
            btnDecrypt.Enabled = false;
        }

        private void GenerateIdBlock()
        {
            if (coverFileExists && messageFileExists)
            {
                var hashBlock = HashGenerator.GetHash(messageFileBytes);
                var generatedIdString = hashBlock + "|" + messageFileType + "|";
                var stegoFileKeyIndex = coverFileBytes.Length.ToString().PadLeft(64 - generatedIdString.Length, '0');
                generatedIdBlock = Encoding.ASCII.GetBytes(generatedIdString + stegoFileKeyIndex);
                // Debug output
                Debug.WriteLine("Hashblock length: "+hashBlock.Length);
                Debug.WriteLine("Stego key index: " + stegoFileKeyIndex);
                Debug.WriteLine("Total Id Block length: " + generatedIdBlock.Length);

                // Unlock 'Hide' button 
                btnEncrypt.Enabled = true;
            }
        }

        private void btnBrowseCover_Click(object sender, EventArgs e)
        {
            openCoverFileDialog.Filter = "Image Files (*.BMP; *.JPG; *.PNG)| *.bmp; *.jpg; *.png | PDF Files (*.pdf) | *.pdf";
            DialogResult openCoverFileDialogResult = openCoverFileDialog.ShowDialog();
            if (openCoverFileDialogResult == DialogResult.OK)
            {
                // Read the cover file into bytes, then close it
                coverFileBytes = File.ReadAllBytes(openCoverFileDialog.FileName);
                lblFileNameCover.Text = openCoverFileDialog.SafeFileName;
                lblCoverFileType.Text = coverFileType = Path.GetExtension(openCoverFileDialog.FileName).Substring(1).ToUpper();
                lblCoverFileSize.Text = (coverFileBytes.LongLength / 1000).ToString() + " KBytes";
                coverFileExists = true; // Tell that cover file exists
            }
            // Attempt to generate id block and unlock form elements
            GenerateIdBlock();
        }

        private void btnBrowseMessage_Click(object sender, EventArgs e)
        {
            DialogResult openMessageFileDialogResult = openMessageFileDialog.ShowDialog();
            if (openMessageFileDialogResult == DialogResult.OK)
            {
                // Read the cover file as bytes, then close it
                messageFileBytes = File.ReadAllBytes(openMessageFileDialog.FileName);
                lblFileNameMessage.Text = openMessageFileDialog.SafeFileName;
                lblMessageFileType.Text = messageFileType = (Path.GetExtension(openMessageFileDialog.FileName).Length > 1) ? Path.GetExtension(openMessageFileDialog.FileName).Substring(1).ToUpper() : "";
                lblMessageFileSize.Text = (messageFileBytes.LongLength / 1000).ToString() + " KBytes";
                messageFileExists = true; // Tell that cover file exists
            }
            // Attempt to generate id block and unlock form elements
            GenerateIdBlock();
        }

        private void btnEncrypt_Click(object sender, EventArgs e)
        {
            if (tbGeneratedKey.Text.Trim().Length < 8)
            {
                MessageBox.Show("Minimum secret passphrase length is 8 characters", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                lblStatus.Text = "Processing...";
                saveStegoFileDialog.Filter = String.Format("{1} file (*.{0})|*.{0}", coverFileType.ToLower(), coverFileType);
                DialogResult saveStegoFileDialogResult = saveStegoFileDialog.ShowDialog();
                if (saveStegoFileDialogResult == DialogResult.OK)
                {
                    try
                    {
                        //Stopwatch sw = Stopwatch.StartNew(); // Stopwatch for benchmarking
                        var cipherBytes = ByteCipher.Encrypt(messageFileBytes, tbGeneratedKey.Text.Trim());
                        //sw.Stop(); // Stop the sw and write the elapsed time
                        //Debug.WriteLine("Total encryption time: "+ sw.ElapsedMilliseconds + " ms");
                        var cipherBytesWithIdBlock = cipherBytes.Concat(ByteCipher.Encrypt(generatedIdBlock, tbGeneratedKey.Text.Trim())).ToArray();
                        //sw.Restart(); // Restart the stopwatch
                        File.WriteAllBytes(saveStegoFileDialog.FileName, coverFileBytes.Concat(cipherBytesWithIdBlock).ToArray());
                        //sw.Stop();
                        //Debug.WriteLine("Total hiding time: " + sw.ElapsedMilliseconds + " ms");
                        DialogResult successDialog = MessageBox.Show("File encrypted and hid successfully", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        if (successDialog == DialogResult.OK && cbOpenStegoFile.Checked)
                        {
                            Process.Start(saveStegoFileDialog.FileName);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        // Reset form visual
                        lblStatus.Text = "Ready";
                        lblFileNameCover.Text = lblFileNameMessage.Text = "No file chosen";
                        lblCoverFileSize.Text = lblCoverFileType.Text = lblMessageFileSize.Text = lblMessageFileType.Text = "-";
                        coverFileExists = messageFileExists = false;
                        btnEncrypt.Enabled = false;
                    }
                }
                else
                {
                    lblStatus.Text = "Ready";
                }
            }
        }

        private void btnBrowseStego_Click(object sender, EventArgs e)
        {
            DialogResult openStegoFileDialogResult = openStegoFileDialog.ShowDialog();
            if (openStegoFileDialogResult == DialogResult.OK)
            {
                stegoFileBytes = File.ReadAllBytes(openStegoFileDialog.FileName);
                lblFileNameStego.Text = openStegoFileDialog.SafeFileName;
                lblStegoFileSize.Text = (stegoFileBytes.LongLength / 1000).ToString() + " KBytes";
                lblStegoFileType.Text = Path.GetExtension(openStegoFileDialog.FileName).Substring(1).ToUpper();
                btnDecrypt.Enabled = true;
            }
        }

        private void btnDecrypt_Click(object sender, EventArgs e)
        {
            if (tbInputKey.Text.Trim().Length < 8)
            {
                MessageBox.Show("Minimum secret passphrase length is 8 characters", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                try
                {
                    lblStatus.Text = "Processing...";
                    var passphrase = tbInputKey.Text.Trim();
                    // Retrieve the Id block as string
                    var retrievedIdBlockString = Encoding.ASCII.GetString(ByteCipher.Decrypt(stegoFileBytes.Skip(stegoFileBytes.Length - 112).ToArray(), passphrase)).Split('|');
                    if (retrievedIdBlockString.Length != 3) throw new Exception("File is corrupted or invalid");

                    var retrievedHash = retrievedIdBlockString[0];
                    var hiddenFileType = retrievedIdBlockString[1].ToLower();
                    var keyindex = Convert.ToInt32(retrievedIdBlockString[2]);

                    var retrievedCipherBytesWithIdBlock = stegoFileBytes.Skip(keyindex).ToArray();
                    var retrievedCipherBytes = retrievedCipherBytesWithIdBlock.Take(retrievedCipherBytesWithIdBlock.Length - 112).ToArray();

                    // Attempt to decrypt
                    var retrievedMessageBytes = ByteCipher.Decrypt(retrievedCipherBytes, passphrase);
                    if (retrievedHash != HashGenerator.GetHash(retrievedMessageBytes)) throw new Exception("File has been modified");

                    // Save the retrieved message file
                    saveExtractedFileDialog.Filter = String.Format("{0} file (*.{1})|*.{1}", hiddenFileType.ToUpper(), hiddenFileType);
                    DialogResult saveExtractedFileDialogResult = saveExtractedFileDialog.ShowDialog();
                    if (saveExtractedFileDialogResult == DialogResult.OK)
                    {
                        File.WriteAllBytes(saveExtractedFileDialog.FileName, retrievedMessageBytes);
                        DialogResult successDialog = MessageBox.Show("File extracted successfully", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        if (successDialog == DialogResult.OK && cbOpenRecoveredFile.Checked)
                        {
                            Process.Start(saveExtractedFileDialog.FileName);
                        }
                    }
                    
                }catch(Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    // Reset the form visual
                    lblStatus.Text = "Ready";
                    lblFileNameStego.Text = "No file chosen";
                    lblStegoFileSize.Text = lblStegoFileType.Text = "-";
                    btnDecrypt.Enabled = false;
                }
            }
        }
    }
}
